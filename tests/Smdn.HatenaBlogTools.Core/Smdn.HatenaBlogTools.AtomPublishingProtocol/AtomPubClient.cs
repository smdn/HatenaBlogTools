// SPDX-FileCopyrightText: 2022 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#nullable enable

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using NUnit.Framework;

namespace Smdn.HatenaBlogTools.AtomPublishingProtocol;

[TestFixture]
public class AtomPubClientTests {
  private readonly PseudoAtomPubServer server = new();

  [OneTimeSetUp]
  public void InitializeTestFixture() => server.Start();

  [OneTimeTearDown]
  public void DisposeTestFixture() => server.Stop();

  private static System.Collections.IEnumerable YieldTestCases_Ctor()
  {
    yield return new object?[] { new NetworkCredential("user", "pass"), "user-agent", null, "both non-null" };
    yield return new object?[] { new NetworkCredential("user", "pass"), null, null, "user agent can be null" };
    yield return new object?[] { new NetworkCredential("user", "pass"), string.Empty, null, "user agent can be empty" };
    yield return new object?[] { null, "user agent", typeof(ArgumentNullException), "credential cannot be null" };
  }

  [TestCaseSource(nameof(YieldTestCases_Ctor))]
  public void Ctor(NetworkCredential? credential, string? userAgent, Type? typeOfExpectedException, string message)
  {
    if (typeOfExpectedException is null)
      Assert.DoesNotThrow(() => new AtomPubClient(credential!, userAgent), message);
    else
      Assert.Throws(typeOfExpectedException, () => new AtomPubClient(credential!, userAgent), message);
  }

  [Test]
  public void Credential_Set()
  {
    var client = new AtomPubClient(new("user", "pass"));

    Assert.DoesNotThrow(() => client.Credential = new NetworkCredential("admin", "pass"));
  }

  [Test]
  public void Credential_SetNull()
  {
    var client = new AtomPubClient(new("user", "pass"));

    Assert.Throws<ArgumentNullException>(() => client.Credential = null!);
  }

  [TestCase("user-agent")]
  [TestCase("")]
  [TestCase(null)]
  public async Task Get_200_OK(string? userAgent)
  {
    const string userName = "user";

    var client = new AtomPubClient(new(userName, "pass"), userAgent: userAgent) {
      Timeout = 1000
    };

    server.SetNextResponseContent(
      status: HttpStatusCode.OK,
      content: @"<?xml version=""1.0"" encoding=""utf-8""?>
<service xmlns=""http://www.w3.org/2007/app"" />"
    );

    var task = server.ProcessRequestAsync();

    HttpStatusCode status = default;
    XDocument? responseDocument= null;

    Assert.DoesNotThrow(() => status = client.Get(server.Url, out responseDocument));

    Assert.That(status, Is.EqualTo(HttpStatusCode.OK));
    Assert.That(responseDocument, Is.Not.Null, nameof(responseDocument));
    Assert.That(responseDocument!.Root!.Name.LocalName, Is.EqualTo("service"));
    Assert.That(responseDocument.Root!.Name.NamespaceName, Is.EqualTo(AtomPub.Namespaces.App.NamespaceName));

    var (request, _) = await task;

    Assert.That(request.HttpMethod, Is.EqualTo(WebRequestMethods.Http.Get), nameof(request.HttpMethod));
    Assert.That(request.UserAgent ?? string.Empty, Is.EqualTo(userAgent ?? string.Empty), nameof(request.UserAgent));
    Assert.That(request.Headers["X-WSSE"], Is.Not.Null, "has X-WSSE header");
    Assert.That(request.Headers["X-WSSE"], Does.Contain($"UsernameToken Username=\"{userName}\""), "X-WSSE header value");

    var trimmedAcceptTypes = request.AcceptTypes?.Select(static t => t?.Trim())?.ToList();

    Assert.That(trimmedAcceptTypes, Has.Member("application/x.atom+xml"));
    Assert.That(trimmedAcceptTypes, Has.Member("application/atom+xml"));
    Assert.That(trimmedAcceptTypes, Has.Member("application/atomsvc+xml"));
    Assert.That(trimmedAcceptTypes, Has.Member("application/xml"));
    Assert.That(trimmedAcceptTypes, Has.Member("text/xml"));
  }

  [Test]
  public async Task Get_400_BadRequest()
  {
    const string userName = "user";

    var client = new AtomPubClient(new(userName, "pass")) {
      Timeout = 1000
    };

    server.SetNextResponseContent(
      status: HttpStatusCode.BadRequest,
      content: ">> 400 Bad Request <<"
    );

    var task = server.ProcessRequestAsync();

    HttpStatusCode status = default;
    XDocument? responseDocument= null;

    Assert.DoesNotThrow(() => status = client.Get(server.Url, out responseDocument));

    Assert.That(status, Is.EqualTo(HttpStatusCode.BadRequest));
    Assert.That(responseDocument, Is.Null, nameof(responseDocument));

    var (request, _) = await task;

    Assert.That(request.HttpMethod, Is.EqualTo(WebRequestMethods.Http.Get), nameof(request.HttpMethod));
    Assert.That(request.Headers["X-WSSE"], Is.Not.Null, "has X-WSSE header");
    Assert.That(request.Headers["X-WSSE"], Does.Contain($"UsernameToken Username=\"{userName}\""), "X-WSSE header value");
  }

  [Test]
  public async Task Post_201_Created()
  {
    var client = new AtomPubClient(new("user", "pass")) {
      Timeout = 1000
    };

    server.SetNextResponseContent(
      status: HttpStatusCode.Created,
      content: @"<?xml version=""1.0"" encoding=""utf-8""?>
<entry xmlns=""http://www.w3.org/2005/Atom"" />"
    );

    var task = server.ProcessRequestAsync();

    HttpStatusCode status = default;
    XDocument? responseDocument= null;

    Assert.DoesNotThrow(() => {
      status = client.Post(
        server.Url,
        new XDocument(
          new XDeclaration("1.0", "utf-8", null),
          new XElement(AtomPub.Namespaces.Atom + "entry")
        ),
        out responseDocument
      );
    });

    Assert.That(status, Is.EqualTo(HttpStatusCode.Created));
    Assert.That(responseDocument, Is.Not.Null, nameof(responseDocument));
    Assert.That(responseDocument!.Root!.Name.LocalName, Is.EqualTo("entry"));
    Assert.That(responseDocument.Root!.Name.NamespaceName, Is.EqualTo(AtomPub.Namespaces.Atom.NamespaceName));

    var (request, _) = await task;

    Assert.That(request.HttpMethod, Is.EqualTo(WebRequestMethods.Http.Post), nameof(request.HttpMethod));
    Assert.That(request.Headers["X-WSSE"], Is.Not.Null, "has X-WSSE header");
  }

  [Test]
  public async Task Post_401_Unauthorized()
  {
    var client = new AtomPubClient(new("user", "pass")) {
      Timeout = 1000
    };

    server.SetNextResponseContent(
      status: HttpStatusCode.Unauthorized,
      content: ">> 401 unauthorized <<"
    );

    var task = server.ProcessRequestAsync();

    HttpStatusCode status = default;
    XDocument? responseDocument= null;

    Assert.DoesNotThrow(() => {
      status = client.Post(
        server.Url,
        new XDocument(
          new XDeclaration("1.0", "utf-8", null),
          new XElement(AtomPub.Namespaces.Atom + "entry")
        ),
        out responseDocument
      );
    });

    Assert.That(status, Is.EqualTo(HttpStatusCode.Unauthorized));
    Assert.That(responseDocument, Is.Null, nameof(responseDocument));

    var (request, _) = await task;

    Assert.That(request.HttpMethod, Is.EqualTo(WebRequestMethods.Http.Post), nameof(request.HttpMethod));
  }

  [Test]
  public async Task Put_200_OK()
  {
    var client = new AtomPubClient(new("user", "pass")) {
      Timeout = 1000
    };

    server.SetNextResponseContent(
      status: HttpStatusCode.OK,
      content: @"<?xml version=""1.0"" encoding=""utf-8""?>
<entry xmlns=""http://www.w3.org/2005/Atom"" />"
    );

    var task = server.ProcessRequestAsync();

    HttpStatusCode status = default;
    XDocument? responseDocument= null;

    Assert.DoesNotThrow(() => {
      status = client.Put(
        server.Url,
        new XDocument(
          new XDeclaration("1.0", "utf-8", null),
          new XElement(AtomPub.Namespaces.Atom + "entry")
        ),
        out responseDocument
      );
    });

    Assert.That(status, Is.EqualTo(HttpStatusCode.OK));
    Assert.That(responseDocument, Is.Not.Null, nameof(responseDocument));
    Assert.That(responseDocument!.Root!.Name.LocalName, Is.EqualTo("entry"));
    Assert.That(responseDocument.Root!.Name.NamespaceName, Is.EqualTo(AtomPub.Namespaces.Atom.NamespaceName));

    var (request, _) = await task;

    Assert.That(request.HttpMethod, Is.EqualTo(WebRequestMethods.Http.Put), nameof(request.HttpMethod));
    Assert.That(request.Headers["X-WSSE"], Is.Not.Null, "has X-WSSE header");
  }

  [Test]
  public async Task Put_405_MethodNotAllowed()
  {
    var client = new AtomPubClient(new("user", "pass")) {
      Timeout = 1000
    };

    server.SetNextResponseContent(
      status: HttpStatusCode.MethodNotAllowed,
      content: ">> 405 put not allowed <<"
    );

    var task = server.ProcessRequestAsync();

    HttpStatusCode status = default;
    XDocument? responseDocument= null;

    Assert.DoesNotThrow(() => {
      status = client.Put(
        server.Url,
        new XDocument(
          new XDeclaration("1.0", "utf-8", null),
          new XElement(AtomPub.Namespaces.Atom + "entry")
        ),
        out responseDocument
      );
    });

    Assert.That(status, Is.EqualTo(HttpStatusCode.MethodNotAllowed));
    Assert.That(responseDocument, Is.Null, nameof(responseDocument));

    var (request, _) = await task;

    Assert.That(request.HttpMethod, Is.EqualTo(WebRequestMethods.Http.Put), nameof(request.HttpMethod));
  }

  [TestCase(true)]
  [TestCase(false)]
  public async Task Post_Put_RequestBodyMustNotContainBOM(bool postIfTrueOtherwisePut)
  {
    var client = new AtomPubClient(new("user", "pass")) {
      Timeout = 1000
    };

    server.SetNextResponseContent(
      status: HttpStatusCode.OK,
      content: @"<?xml version=""1.0"" encoding=""utf-8""?>
<entry xmlns=""http://www.w3.org/2005/Atom"" />"
    );

    var task = server.ProcessRequestAsync();

    var requestDocument = new XDocument(
      new XDeclaration("1.0", "utf-8", null),
      new XElement(AtomPub.Namespaces.Atom + "entry")
    );
    HttpStatusCode status = default;
    XDocument? responseDocument= null;

    Assert.DoesNotThrow(() => {
      status = postIfTrueOtherwisePut
        ? client.Post(server.Url, requestDocument, out responseDocument)
        : client.Put(server.Url, requestDocument, out responseDocument);
    });

    var (request, requestStream) = await task;

    Assert.That(
      request.HttpMethod,
      Is.EqualTo(postIfTrueOtherwisePut
        ? WebRequestMethods.Http.Post
        : WebRequestMethods.Http.Put),
      nameof(request.HttpMethod)
    );

    var requestBodyFirst5Bytes = new byte[5];

    requestStream.Read(requestBodyFirst5Bytes, 0, requestBodyFirst5Bytes.Length);

    Assert.That(
      requestBodyFirst5Bytes,
      Is.EqualTo(new byte[] { (byte)'<', (byte)'?', (byte)'x', (byte)'m', (byte)'l' }).AsCollection,
      "request body must be sent in UTF-8 without BOM"
    );
  }
}
