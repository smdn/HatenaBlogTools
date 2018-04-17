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

using Smdn.Text;
using Smdn.Xml;
using Smdn.Xml.Linq;

namespace Smdn.Applications.HatenaBlogTools {
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

  public class HatenaBlogAtomPub {
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

    private Atom atom = null;

    private static Uri GetRootEndPont(string hatenaId, string blogId, string apiKey)
    {
      return new Uri(string.Concat("https://blog.hatena.ne.jp/", hatenaId, "/", blogId, "/atom"));
    }

    public HatenaBlogAtomPub(string hatenaId, string blogId, string apiKey)
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

    private Atom EnsureInitAtomClient()
    {
      if (atom != null)
        return atom;

      atom = new Atom();
      atom.Credential = new NetworkCredential(HatenaId, apiKey);

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

  [Obsolete]
  public static class HatenaBlog {
    private static XmlDocument ToXmlDocument(this XDocument document)
    {
      var ret = new XmlDocument();

      ret.LoadXml(document.ToString());

      return ret;
    }

    public static void WaitForCinnamon()
    {
      System.Threading.Thread.Sleep(250);
    }

    public static XmlDocument GetServiceDocuments(string hatenaId, string blogId, string apiKey, out HttpStatusCode statusCode)
    {
      var atom = new Atom();

      atom.Credential = new NetworkCredential(hatenaId, apiKey);

      ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
      ServicePointManager.ServerCertificateValidationCallback = (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => {
#if DEBUG
        Console.Error.WriteLine(sslPolicyErrors);
#endif

        if (sslPolicyErrors == SslPolicyErrors.None)
          return true;
        else
          return false;
      };

      var rootEndPoint = new Uri(string.Concat("https://blog.hatena.ne.jp/", hatenaId, "/", blogId, "/atom"));

      statusCode = atom.Get(rootEndPoint, out XDocument responseDocument);

      return responseDocument.ToXmlDocument();
    }

    public static List<PostedEntry> GetEntries(string hatenaId, string blogId, string apiKey)
    {
      return new List<PostedEntry>(EnumerateEntries(hatenaId, blogId, apiKey));
    }

    public static IEnumerable<PostedEntry> EnumerateEntries(string hatenaId, string blogId, string apiKey)
    {
      var atom = new Atom();

      atom.Credential = new NetworkCredential(hatenaId, apiKey);

      var rootEndPoint = new Uri(string.Concat("http://blog.hatena.ne.jp/", hatenaId, "/", blogId, "/atom/"));
      var nextUri = new Uri(rootEndPoint, "./entry");

      for (;;) {
        var statusCode = atom.Get(nextUri, out XDocument responseDocument);
        var collectionDocument = responseDocument.ToXmlDocument();

        if (statusCode != HttpStatusCode.OK)
          throw new WebException(string.Format("エントリの取得に失敗したため中断しました ({0})", statusCode), WebExceptionStatus.ProtocolError);

        foreach (var entry in ReadEntries(collectionDocument)) {
          yield return entry;
        }

        // 次のatom:linkを取得する
        var nsmgr = new XmlNamespaceManager(collectionDocument.NameTable);

        nsmgr.AddNamespace("atom", Namespaces.Atom);

        nextUri = collectionDocument.GetSingleNodeValueOf("/atom:feed/atom:link[@rel='next']/@href", nsmgr, s => s == null ? null : new Uri(s));

        if (nextUri == null)
          break;
      }
    }

    private static IEnumerable<PostedEntry> ReadEntries(XmlDocument doc)
    {
      var nsmgr = new XmlNamespaceManager(doc.NameTable);

      nsmgr.PushScope();
      nsmgr.AddNamespace("atom", Namespaces.Atom);
      nsmgr.AddNamespace("app", Namespaces.App);

      foreach (XmlNode entry in doc.SelectNodes("/atom:feed/atom:entry", nsmgr)) {
        var e = new PostedEntry();

        e.MemberUri = entry.GetSingleNodeValueOf("atom:link[@rel='edit']/@href", nsmgr, s => s == null ? null : new Uri(s));
        e.Title = entry.GetSingleNodeValueOf("atom:title/text()", nsmgr);
        e.Author = entry.GetSingleNodeValueOf("atom:author/atom:name/text()", nsmgr);
        e.Content = entry.GetSingleNodeValueOf("atom:content/text()", nsmgr);

        try {
          e.Published = entry.GetSingleNodeValueOf<DateTimeOffset>("atom:published/text()", nsmgr, DateTimeOffset.Parse); // XXX
        }
        catch (FormatException) {
          // ignore exception
        }

        try {
          e.Updated = entry.GetSingleNodeValueOf<DateTimeOffset>("atom:updated/text()", nsmgr, DateTimeOffset.Parse); // XXX
        }
        catch (FormatException) {
          // ignore exception
        }

        foreach (XmlElement category in entry.SelectNodes("./atom:category", nsmgr)) {
          e.Categories.Add(category.GetAttribute("term"));
        }

        e.IsDraft = entry.GetSingleNodeValueOf<bool>("app:control/app:draft/text()", nsmgr, IsDraftYes);

        yield return e;
      }

      nsmgr.PopScope();
    }

    private static bool IsDraftYes(string str)
    {
      return string.Equals(str, "yes", StringComparison.OrdinalIgnoreCase);
    }

    public static XmlDocument UpdateEntry(Atom atom, PostedEntry updatingEntry, out HttpStatusCode statusCode)
    {
      statusCode = default(HttpStatusCode);

      var putDocument = CreatePostDocument(updatingEntry);

      if (updatingEntry.Author != null)
        putDocument.DocumentElement.AppendElement("author", Namespaces.Atom).AppendElement("name", Namespaces.Atom).AppendText(updatingEntry.Author);

      return atom.Put(updatingEntry.MemberUri, putDocument, out statusCode).ToXmlDocument();
    }

    public static XmlDocument PostEntry(Atom atom, Uri collectionUri, Entry entry, out HttpStatusCode statusCode)
    {
      statusCode = default(HttpStatusCode);

      var postDocument = CreatePostDocument(entry);

      return atom.Post(collectionUri, postDocument, out statusCode).ToXmlDocument();
    }

    private static XmlDocument CreatePostDocument(Entry entry)
    {
      var document = new XmlDocument();

      document.AppendChild(document.CreateXmlDeclaration("1.0", "utf-8", null));

      var e = document.AppendElement("entry", Namespaces.Atom);

      e.SetAttribute("xmlns", Namespaces.Atom);
      e.SetAttribute("xmlns:app", Namespaces.App);

      e.AppendElement("title", Namespaces.Atom).AppendText(entry.Title);

      if (entry.Updated.HasValue)
        e.AppendElement("updated", Namespaces.Atom).AppendText(XmlConvert.ToString(entry.Updated.Value));

      var c = e.AppendElement("content", Namespaces.Atom);

      c.SetAttribute("type", "text/plain");
      c.AppendText(entry.Content);

      foreach (var category in entry.Categories) {
        e.AppendElement("category", Namespaces.Atom).SetAttribute("term", category);
      }

      if (entry.IsDraft)
        e.AppendElement("control", Namespaces.App).AppendElement("draft", Namespaces.App).AppendText("yes");

      return document;
    }
  }
}
