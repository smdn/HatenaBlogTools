//
// Author:
//       smdn <smdn@smdn.jp>
//
// Copyright (c) 2013-2014 smdn
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.Xml.Linq;

using Smdn.Applications.HatenaBlogTools.AtomPublishingProtocol;
using Smdn.Text;
using Smdn.Xml.Linq;

namespace Smdn.Applications.HatenaBlogTools.HatenaBlog {
  public class Entry {
    public string Title;
    public HashSet<string> Categories = new HashSet<string>(StringComparer.Ordinal);
    public DateTimeOffset? Updated;
    public bool IsDraft;
    public string Content;
  }

  public class PostedEntry : Entry {
    public Uri MemberUri;
    public Uri EntryUri;
    public string Author;
    public DateTimeOffset Published;
    public string FormattedContent;
  }

  public class HatenaBlogAtomPubClient {
    public static void InitializeHttpsServicePoint()
    {
      ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
      ServicePointManager.ServerCertificateValidationCallback = (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => {
        if (sslPolicyErrors == SslPolicyErrors.None) {
          return true;
        }
        else {
          Console.Error.WriteLine(sslPolicyErrors);

          return false;
        }
      };
    }

    public string HatenaId {
      get; private set;
    }

    public string BlogId {
      get; private set;
    }

    private readonly string apiKey;

    public Uri RootEndPoint {
      get; private set;
    }

    public string BlogTitle {
      get; private set;
    }

    public Uri CollectionUri {
      get; private set;
    }

    private string userAgent;

    public string UserAgent {
      get { return atom?.UserAgent ?? userAgent; }
      set {
        userAgent = value;

        if (atom != null)
          atom.UserAgent = value;
      }
    }

    private AtomPubClient atom = null;

    private static Uri GetRootEndPont(string hatenaId, string blogId, string apiKey)
    {
      return new Uri(string.Concat("https://blog.hatena.ne.jp/", hatenaId, "/", blogId, "/atom"));
    }

    public HatenaBlogAtomPubClient(string hatenaId, string blogId, string apiKey)
    {
      if (string.IsNullOrEmpty(hatenaId))
        throw new ArgumentException("must be non empty value", nameof(hatenaId));
      if (string.IsNullOrEmpty(blogId))
        throw new ArgumentException("must be non empty value", nameof(blogId));
      if (string.IsNullOrEmpty(apiKey))
        throw new ArgumentException("must be non empty value", nameof(apiKey));

      this.HatenaId = hatenaId;
      this.BlogId = blogId;
      this.apiKey = apiKey;
      this.RootEndPoint = GetRootEndPont(hatenaId, blogId, apiKey);
    }

    public void WaitForCinnamon()
    {
      System.Threading.Thread.Sleep(250);
    }

    private AtomPubClient EnsureInitAtomClient()
    {
      if (atom != null)
        return atom;

      atom = new AtomPubClient();
      atom.Credential = new NetworkCredential(HatenaId, apiKey);
      atom.UserAgent = userAgent;

      return atom;
    }

    public HttpStatusCode Login(out XDocument serviceDocument)
    {
      return GetServiceDocuments(out serviceDocument);
    }

    private HttpStatusCode GetServiceDocuments(out XDocument serviceDocument)
    {
      var statusCode = EnsureInitAtomClient().Get(RootEndPoint, out serviceDocument);

      if (statusCode != HttpStatusCode.OK)
        return statusCode;

      if (serviceDocument.Root.Name != AtomPub.ElementNames.AppService)
        throw new NotSupportedException($"unexpected document type: {serviceDocument.Root.Name}");

      BlogTitle = serviceDocument.Root
                                 .Element(AtomPub.ElementNames.AppWorkspace)
                                 ?.Element(AtomPub.ElementNames.AtomTitle)
                                 ?.Value;

      CollectionUri = serviceDocument.Root
                                     .Element(AtomPub.ElementNames.AppWorkspace)
                                     ?.Elements(AtomPub.ElementNames.AppCollection)
                                     ?.FirstOrDefault(e => e.Element(AtomPub.ElementNames.AppAccept).Value.Contains("type=entry"))
                                     ?.GetAttributeValue("href", StringConversion.ToUri);

      return statusCode;
    }

    public IEnumerable<PostedEntry> EnumerateEntries()
    {
      foreach (var pair in EnumerateAllEntries()) {
        yield return pair.Item1;
      }
    }

    public void EnumerateEntries(Action<PostedEntry, XElement> actionForEachEntry)
    {
      if (actionForEachEntry == null)
        throw new ArgumentNullException(nameof(actionForEachEntry));

      foreach (var pair in EnumerateAllEntries()) {
        actionForEachEntry(pair.Item1, pair.Item2);
      }
    }

    private IEnumerable<Tuple<PostedEntry, XElement>> EnumerateAllEntries()
    {
      if (atom == null)
        throw new InvalidOperationException("not logged in");

      var nextUri = CollectionUri;

      for (;;) {
        var statusCode = atom.Get(nextUri, out XDocument collectionDocument);

        if (statusCode != HttpStatusCode.OK)
          throw new WebException(string.Format("エントリの取得に失敗したため中断しました ({0})", statusCode), WebExceptionStatus.ProtocolError);

        foreach (var entry in ReadEntries(collectionDocument)) {
          yield return entry;
        }

        // 次のatom:linkを取得する
        nextUri = collectionDocument.Element(AtomPub.Namespaces.Atom + "feed")
                                    ?.Elements(AtomPub.Namespaces.Atom + "link")
                                    ?.FirstOrDefault(e => e.HasAttributeWithValue("rel", "next"))
                                    ?.GetAttributeValue("href", StringConversion.ToUriNullable);

        if (nextUri == null)
          break;
      }
    }

    private static IEnumerable<Tuple<PostedEntry, XElement>> ReadEntries(XDocument doc)
    {
      return doc.Element(AtomPub.Namespaces.Atom + "feed")
                ?.Elements(AtomPub.Namespaces.Atom + "entry")
                ?.Select(entry => Tuple.Create(ConvertEntry(entry), entry)) ?? Enumerable.Empty<Tuple<PostedEntry, XElement>>();

      PostedEntry ConvertEntry(XElement entry)
      {
        var e = new PostedEntry();

        e.MemberUri = entry
          .Elements(AtomPub.Namespaces.Atom + "link")
          .FirstOrDefault(link => link.HasAttributeWithValue("rel", "edit"))
          ?.GetAttributeValue("href", StringConversion.ToUriNullable);

        e.EntryUri = entry
          .Elements(AtomPub.Namespaces.Atom + "link")
          .FirstOrDefault(link => link.HasAttributeWithValue("rel", "alternate") && link.HasAttributeWithValue("type", "text/html"))
          ?.GetAttributeValue("href", StringConversion.ToUriNullable);

        e.Title = entry.Element(AtomPub.Namespaces.Atom + "title")?.Value;
        e.Author = entry.Element(AtomPub.Namespaces.Atom + "author")?.Element(AtomPub.Namespaces.Atom + "name")?.Value;
        e.Content = entry.Element(AtomPub.Namespaces.Atom + "content")?.Value;
        e.FormattedContent = entry.Element(AtomPub.Namespaces.Hatena + "formatted-content")?.Value;

        try {
          e.Published = DateTimeOffset.Parse(entry.Element(AtomPub.Namespaces.Atom + "published")?.Value);
        }
        catch (ArgumentNullException) {
          // ignore exception
        }
        catch (FormatException) {
          // ignore exception
        }

        try {
          e.Updated = DateTimeOffset.Parse(entry.Element(AtomPub.Namespaces.Atom + "updated")?.Value);
        }
        catch (ArgumentNullException) {
          // ignore exception
        }
        catch (FormatException) {
          // ignore exception
        }

        foreach (var category in entry.Elements(AtomPub.Namespaces.Atom + "category").Select(c => c.GetAttributeValue("term"))) {
          e.Categories.Add(category);
        }

        e.IsDraft = IsYes(entry.Element(AtomPub.Namespaces.App + "control")?.Element(AtomPub.Namespaces.App + "draft")?.Value);

        return e;
      }

      bool IsYes(string str)
      {
        return string.Equals(str, "yes", StringComparison.OrdinalIgnoreCase);
      }
    }

    public HttpStatusCode UpdateEntry(PostedEntry updatingEntry, out XDocument responseDocument)
    {
      if (atom == null)
        throw new InvalidOperationException("not logged in");

      responseDocument = null;

      var putDocument = CreatePostDocument(updatingEntry);

      if (updatingEntry.Author != null) {
        putDocument.Root.Add(new XElement(
          AtomPub.Namespaces.Atom + "author",
          new XElement(
            AtomPub.Namespaces.Atom + "name",
            new XText(updatingEntry.Author)
          )
        ));
      }

      return atom.Put(updatingEntry.MemberUri, putDocument, out responseDocument);
    }

    public HttpStatusCode PostEntry(Entry entry, out XDocument responseDocument)
    {
      if (atom == null)
        throw new InvalidOperationException("not logged in");

      return atom.Post(CollectionUri, CreatePostDocument(entry), out responseDocument);
    }

    private static XDocument CreatePostDocument(Entry postEntry)
    {
      var entry = new XElement(
        AtomPub.Namespaces.Atom + "entry",
        new XElement(
          AtomPub.Namespaces.Atom + "title",
          new XText(postEntry.Title)
        ),
        postEntry.Updated.HasValue
          ? new XElement(
            AtomPub.Namespaces.Atom + "updated",
            new XText(XmlConvert.ToString(postEntry.Updated.Value))
          )
          : null,
        new XElement(
          AtomPub.Namespaces.Atom + "content",
          new XAttribute("type", "text/plain"),
          new XText(postEntry.Content)
        ),
        postEntry.Categories.Select(c => new XElement(
          AtomPub.Namespaces.Atom + "category",
          new XAttribute("term", c)
        )),
        new XElement(
          AtomPub.Namespaces.App + "control",
          new XElement(
            AtomPub.Namespaces.App + "draft",
            new XText(postEntry.IsDraft ? "yes" : "no")
          )
        )
      );

      return new XDocument(new XDeclaration("1.0", "utf-8", null),
                           entry);
    }
  }
}
