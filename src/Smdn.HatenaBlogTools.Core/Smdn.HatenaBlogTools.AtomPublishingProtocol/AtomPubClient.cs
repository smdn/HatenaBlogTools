// SPDX-FileCopyrightText: 2013 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using Smdn.Net;

namespace Smdn.HatenaBlogTools.AtomPublishingProtocol;

public class AtomPubClient {
  public static readonly int DefaultTimeoutMilliseconds = 20 * 1000;

  public AtomPubClient(NetworkCredential credential, string? userAgent = null)
  {
    this.credential = credential ?? throw new ArgumentNullException(nameof(credential));
    UserAgent = userAgent;
  }

  private int timeout = DefaultTimeoutMilliseconds;

  public int Timeout {
    get => timeout;
    set {
      if (value < -1)
        throw ExceptionUtils.CreateArgumentMustBeGreaterThanOrEqualTo(-1, nameof(Timeout), value);

      timeout = value;
    }
  }

  private NetworkCredential credential;

  public NetworkCredential Credential {
    get => credential;
    set => credential = value ?? throw new ArgumentNullException(nameof(Credential));
  }

  public string? UserAgent { get; set; }

  public HttpStatusCode Get(Uri requestUri, out XDocument? responseDocument)
    => GetResponse(() => CreateRequest(WebRequestMethods.Http.Get, requestUri), out responseDocument);

  public HttpStatusCode Post(Uri requestUri, XDocument requestDocument, out XDocument? responseDocument)
    => PostPut(WebRequestMethods.Http.Post, requestUri, requestDocument, out responseDocument);

  public HttpStatusCode Put(Uri requestUri, XDocument requestDocument, out XDocument? responseDocument)
    => PostPut(WebRequestMethods.Http.Put, requestUri, requestDocument, out responseDocument);

  private HttpStatusCode PostPut(string method, Uri requestUri, XDocument requestDocument, out XDocument? responseDocument)
  {
    return GetResponse(
      () => {
        var req = CreateRequest(method, requestUri);

        req.ContentType = "application/atom+xml";

        var settings = new XmlWriterSettings() {
          Encoding = new UTF8Encoding(false), // Hatena blog AtomPub API's XML parser does not accept XML documents with BOM
          NewLineChars = "\n",
          ConformanceLevel = ConformanceLevel.Document,
          Indent = true,
          IndentChars = " ",
          CloseOutput = false,
        };

        using (var reqStream = req.GetRequestStream()) {
          using var writer = XmlWriter.Create(reqStream, settings);
          requestDocument.Save(writer);
        }

        return req;
      },
      out responseDocument
    );
  }

  private HttpWebRequest CreateRequest(string method, Uri requestUri)
  {
    var req = WebRequest.CreateHttp(requestUri);

    req.Method = method;
    req.SetWsseHeader(Credential);
    req.Accept = "application/x.atom+xml, application/atom+xml, application/atomsvc+xml, application/xml, text/xml, */*";
    req.Timeout = timeout;
    req.ReadWriteTimeout = timeout;

    if (UserAgent is not null)
      req.UserAgent = UserAgent;

    return req;
  }

  private static readonly int maxTimeoutRetryCount = 3;

  private static HttpStatusCode GetResponse(Func<HttpWebRequest> createRequest, out XDocument? responseDocument)
  {
    responseDocument = null;

    for (var timeoutRetryCount = maxTimeoutRetryCount; ;) {
      try {
        using var response = GetResponseCore(createRequest());

        if (2 == ((int)response.StatusCode) / 100) { // 2XX
          using var responseStream = response.GetResponseStream();

          responseDocument = XDocument.Load(responseStream);
        }
        else {
          Console.Error.WriteLine(
            "{0} {1} ({2} {3})",
            (int)response.StatusCode,
            response.StatusDescription,
            response.Method,
            response.ResponseUri
          );

#if false
            Console.Error.WriteLine("[response headers]");
            foreach (string h in response.Headers.Keys) {
              Console.Error.WriteLine("{0}: {1}", h, response.Headers[h]);
            }
#endif
          // try read response body
          // XXX: cannot read chunked response with GetResponseStream() (?)
          // XXX: or cannot read empty response (?)
          try {
            using var memoryStream = new MemoryStream();
            using (var respStream = response.GetResponseStream()) {
              respStream.CopyTo(memoryStream);
            }

            memoryStream.Position = 0L;

            using var reader = new StreamReader(memoryStream, Encoding.UTF8);
            Console.Error.WriteLine(reader.ReadToEnd());
          }
          catch {
            // ignore exceptions
          }
        }

        return response.StatusCode;
        // using response
      }
      catch (TimeoutException) {
        if (0 < timeoutRetryCount--)
          continue;
        else
          throw;
      }
    }

    static HttpWebResponse GetResponseCore(HttpWebRequest req)
    {
#if false
      Console.Error.WriteLine("[request headers]");
      foreach (string h in req.Headers.Keys) {
        Console.Error.WriteLine("{0}: {1}", h, req.Headers[h]);
      }
#endif

      HttpWebResponse? resp = null;

      try {
        resp = req.GetResponse() as HttpWebResponse;
      }
      catch (WebException ex) when (ex.Status == WebExceptionStatus.Timeout) {
        throw new TimeoutException("request timed out", ex);
      }
      catch (WebException ex) when (ex.Status == WebExceptionStatus.ProtocolError) {
        resp = ex.Response as HttpWebResponse;
      }

      return resp ?? throw new InvalidOperationException("could not get HTTP response");
    }
  }
}
