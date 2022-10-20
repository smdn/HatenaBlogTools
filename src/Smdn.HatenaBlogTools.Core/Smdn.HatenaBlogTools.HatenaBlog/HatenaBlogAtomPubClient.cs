// SPDX-FileCopyrightText: 2013 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.Xml.Linq;

using Smdn.HatenaBlogTools.AtomPublishingProtocol;
using Smdn.Xml.Linq;

namespace Smdn.HatenaBlogTools.HatenaBlog;

public static class EntryContentType {
  public static readonly string Default = "text/plain";
  public static readonly string HatenaSyntax = "text/x-hatena-syntax";
  public static readonly string Markdown = "text/x-markdown";
  public static readonly string Html = "text/html";

  public static string GetFileExtension(string contentType)
  {
    if (string.Equals(contentType, HatenaSyntax, StringComparison.OrdinalIgnoreCase))
      return ".hatena.txt";
    if (string.Equals(contentType, Markdown, StringComparison.OrdinalIgnoreCase))
      return ".md";
    if (string.Equals(contentType, Html, StringComparison.OrdinalIgnoreCase))
      return ".html";
    if (string.Equals(contentType, Default, StringComparison.OrdinalIgnoreCase))
      return ".txt";

    return null;
  }
}

public class HatenaBlogAtomPubCredential {
  public string HatenaId { get; }
  public string BlogId { get; }
  public string ApiKey { get; }

  public HatenaBlogAtomPubCredential(string hatenaId, string blogId, string apiKey)
  {
    if (string.IsNullOrEmpty(hatenaId))
      throw new ArgumentException("must be non-empty string", nameof(hatenaId));
    if (string.IsNullOrEmpty(blogId))
      throw new ArgumentException("must be non-empty string", nameof(blogId));
    if (string.IsNullOrEmpty(apiKey))
      throw new ArgumentException("must be non-empty string", nameof(apiKey));

    this.HatenaId = hatenaId;
    this.BlogId = blogId;
    this.ApiKey = apiKey;
  }
}

public class PostEntryFailedException : Exception {
  public Entry CausedEntry { get; }

  public PostEntryFailedException(Entry causedEntry, Exception innerException)
    : base("exception occured while posting entry", innerException)
  {
    this.CausedEntry = causedEntry;
  }
}

public abstract class HatenaBlogAtomPubClient {
  public static void InitializeHttpsServicePoint()
  {
    ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault;

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

  public static HatenaBlogAtomPubClient Create(HatenaBlogAtomPubCredential credential)
  {
    return new DefaultHatenaBlogAtomPubClient(credential);
  }

#if DEBUG
  public static HatenaBlogAtomPubClient Create(IReadOnlyList<PostedEntry> entries)
  {
    return new PseudoHatenaBlogAtomPubClient(entries);
  }
#endif

  public static IEnumerable<PostedEntry> ReadEntriesFrom(XDocument document)
  {
    foreach (var entry in DefaultHatenaBlogAtomPubClient.ReadEntries(document)) {
      yield return entry.Item1;
    }
  }

  /*
   * instance members
   */
  public abstract string HatenaId { get; }
  public abstract string BlogId { get; }
  public abstract Uri RootEndPoint { get; }
  public abstract string BlogTitle { get; }
  public abstract Uri CollectionUri { get; }
  public abstract string UserAgent { get; set; }

  public abstract void WaitForCinnamon();

  public abstract HttpStatusCode Login(out XDocument serviceDocument);

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

  protected abstract IEnumerable<Tuple<PostedEntry, XElement>> EnumerateAllEntries();

  public abstract HttpStatusCode UpdateEntry(PostedEntry updatingEntry, out XDocument responseDocument);

  public abstract HttpStatusCode PostEntry(Entry entry, out XDocument responseDocument);
}

internal class DefaultHatenaBlogAtomPubClient : HatenaBlogAtomPubClient {
  private static Uri ToUriNullable(string val) => (val == null) ? null : new Uri(val);

  private readonly HatenaBlogAtomPubCredential credential;

  public override string HatenaId => credential.HatenaId;
  public override string BlogId => credential.BlogId;

  private readonly Uri rootEndPoint;
  public override Uri RootEndPoint => rootEndPoint;

  private string blogTitle;
  public override string BlogTitle => blogTitle;

  private Uri collectionUri;
  public override Uri CollectionUri => collectionUri;

  private string userAgent;

  public override string UserAgent {
    get { return atom?.UserAgent ?? userAgent; }
    set {
      userAgent = value;

      if (atom != null)
        atom.UserAgent = value;
    }
  }

  private AtomPubClient atom = null;

  private static Uri GetRootEndPont(string hatenaId, string blogId)
  {
    return new Uri(string.Concat("https://blog.hatena.ne.jp/", hatenaId, "/", blogId, "/atom"));
  }

  internal DefaultHatenaBlogAtomPubClient(HatenaBlogAtomPubCredential credential)
  {
    if (credential == null)
      throw new ArgumentNullException(nameof(credential));

    this.credential = credential;
    this.rootEndPoint = GetRootEndPont(credential.HatenaId, credential.BlogId);
  }

  public override void WaitForCinnamon()
  {
    System.Threading.Thread.Sleep(250);
  }

  private AtomPubClient EnsureInitAtomClient()
  {
    if (atom != null)
      return atom;

    atom = new AtomPubClient();
    atom.Credential = new NetworkCredential(credential.HatenaId, credential.ApiKey);
    atom.UserAgent = userAgent;

    return atom;
  }

  public override HttpStatusCode Login(out XDocument serviceDocument)
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

    this.blogTitle = serviceDocument.Root
                                    .Element(AtomPub.ElementNames.AppWorkspace)
                                    ?.Element(AtomPub.ElementNames.AtomTitle)
                                    ?.Value;

    this.collectionUri = serviceDocument.Root
                                        .Element(AtomPub.ElementNames.AppWorkspace)
                                        ?.Elements(AtomPub.ElementNames.AppCollection)
                                        ?.FirstOrDefault(e => e.Element(AtomPub.ElementNames.AppAccept).Value.Contains("type=entry"))
                                        ?.GetAttributeValue("href", static val => new Uri(val));

    return statusCode;
  }

  protected override IEnumerable<Tuple<PostedEntry, XElement>> EnumerateAllEntries()
  {
    if (atom == null)
      throw new InvalidOperationException("not logged in");

    var nextUri = CollectionUri;

    for (; ; ) {
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
                                  ?.GetAttributeValue("href", ToUriNullable);

      if (nextUri == null)
        break;

      WaitForCinnamon();
    }
  }

  internal static IEnumerable<Tuple<PostedEntry, XElement>> ReadEntries(XDocument doc)
  {
    return doc.Element(AtomPub.Namespaces.Atom + "feed")
              ?.Elements(AtomPub.Namespaces.Atom + "entry")
              ?.Select(entry => Tuple.Create(ConvertEntry(entry), entry)) ?? Enumerable.Empty<Tuple<PostedEntry, XElement>>();

    PostedEntry ConvertEntry(XElement entry)
    {
      /*
       * posted-entry only propeties
       */
      var memberUri = entry
        .Elements(AtomPub.Namespaces.Atom + "link")
        .FirstOrDefault(link => link.HasAttributeWithValue("rel", "edit"))
        ?.GetAttributeValue("href", ToUriNullable);
      var entryUri = entry
        .Elements(AtomPub.Namespaces.Atom + "link")
        .FirstOrDefault(link => link.HasAttributeWithValue("rel", "alternate") && link.HasAttributeWithValue("type", "text/html"))
        ?.GetAttributeValue("href", ToUriNullable);
      var id = ToUriNullable(entry.Element(AtomPub.Namespaces.Atom + "id")?.Value);
      var formattedContent = entry.Element(AtomPub.Namespaces.Hatena + "formatted-content")?.Value;
      var authors = entry
        .Elements(AtomPub.Namespaces.Atom + "author")
        .Select(elementAuthor => elementAuthor.Element(AtomPub.Namespaces.Atom + "name")?.Value);
      var datePublished = DateTimeOffset.MinValue;

      try {
        datePublished = DateTimeOffset.Parse(entry.Element(AtomPub.Namespaces.Atom + "published")?.Value);
      }
      catch (ArgumentNullException) {
        // ignore exception
      }
      catch (FormatException) {
        // ignore exception
      }

      var e = new PostedEntry(
        id: id,
        memberUri: memberUri,
        entryUri: entryUri,
        datePublished: datePublished,
        authors: authors,
        formattedContent: formattedContent
      );

      /*
       * basic propeties
       */
      e.Title = entry.Element(AtomPub.Namespaces.Atom + "title")?.Value;
      e.Summary = entry.Element(AtomPub.Namespaces.Atom + "summary")?.Value;
      e.Content = entry.Element(AtomPub.Namespaces.Atom + "content")?.Value;
      e.ContentType = entry.Element(AtomPub.Namespaces.Atom + "content")?.GetAttributeValue("type");

      try {
        e.DateUpdated = DateTimeOffset.Parse(entry.Element(AtomPub.Namespaces.Atom + "updated")?.Value);
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

  public override HttpStatusCode UpdateEntry(PostedEntry updatingEntry, out XDocument responseDocument)
  {
    if (atom == null)
      throw new InvalidOperationException("not logged in");

    responseDocument = null;

    try {
      var putDocument = CreatePostDocument(updatingEntry);

      putDocument.Root.Add(
        updatingEntry.Authors.Select(author =>
          string.IsNullOrEmpty(author)
            ? null
            : new XElement(
                AtomPub.Namespaces.Atom + "author",
                new XElement(
                  AtomPub.Namespaces.Atom + "name",
                  author
                )
              )
        )
      );

      return atom.Put(updatingEntry.MemberUri, putDocument, out responseDocument);
    }
    catch (Exception ex) {
      throw new PostEntryFailedException(updatingEntry, ex);
    }
  }

  public override HttpStatusCode PostEntry(Entry entry, out XDocument responseDocument)
  {
    if (atom == null)
      throw new InvalidOperationException("not logged in");

    try {
      return atom.Post(CollectionUri, CreatePostDocument(entry), out responseDocument);
    }
    catch (Exception ex) {
      throw new PostEntryFailedException(entry, ex);
    }
  }

  private static XDocument CreatePostDocument(Entry postEntry)
  {
    var entry = new XElement(
      AtomPub.Namespaces.Atom + "entry",
      new XElement(
        AtomPub.Namespaces.Atom + "title",
        new XText(postEntry.Title)
      ),
      postEntry.DateUpdated.HasValue
        ? new XElement(
          AtomPub.Namespaces.Atom + "updated",
          new XText(XmlConvert.ToString(postEntry.DateUpdated.Value))
        )
        : null,
      postEntry.Summary == null
        ? null
        : new XElement(
          AtomPub.Namespaces.Atom + "summary",
          new XAttribute("type", "text"),
          new XText(postEntry.Summary)
        ),
      new XElement(
        AtomPub.Namespaces.Atom + "content",
#if false
        new XAttribute("type", postEntry.ContentType ?? EntryContentType.Default),
#endif
        new XAttribute("type", EntryContentType.Default),
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

#if DEBUG
internal class PseudoHatenaBlogAtomPubClient : HatenaBlogAtomPubClient {
  private readonly IReadOnlyList<PostedEntry> entries;

  internal PseudoHatenaBlogAtomPubClient(IReadOnlyList<PostedEntry> entries)
  {
    this.entries = entries;
  }

  public override string HatenaId => string.Empty;
  public override string BlogId => string.Empty;
  public override Uri RootEndPoint => throw new NotImplementedException();
  public override string BlogTitle => string.Empty;
  public override Uri CollectionUri => throw new NotImplementedException();
  public override string UserAgent { get; set; }

  public override HttpStatusCode Login(out XDocument serviceDocument)
  {
    serviceDocument = new XDocument();

    return HttpStatusCode.OK;
  }

  public override HttpStatusCode PostEntry(Entry entry, out XDocument responseDocument)
  {
    responseDocument = new XDocument();

    return HttpStatusCode.OK;
  }

  public override HttpStatusCode UpdateEntry(PostedEntry updatingEntry, out XDocument responseDocument)
  {
    responseDocument = new XDocument();

    return HttpStatusCode.OK;
  }

  public override void WaitForCinnamon()
  {
    // do nothing
  }

  protected override IEnumerable<Tuple<PostedEntry, XElement>> EnumerateAllEntries()
  {
    foreach (var entry in entries) {
      yield return Tuple.Create(entry, new XElement("null"));
    }
  }
}
#endif
