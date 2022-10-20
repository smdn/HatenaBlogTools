// SPDX-FileCopyrightText: 2013 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;

using Smdn.HatenaBlogTools.AtomPublishingProtocol;
using Smdn.Xml.Linq;

namespace Smdn.HatenaBlogTools.HatenaBlog;

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
    get => atom?.UserAgent ?? userAgent;
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
    this.credential = credential ?? throw new ArgumentNullException(nameof(credential));
    rootEndPoint = GetRootEndPont(credential.HatenaId, credential.BlogId);
  }

  public override void WaitForCinnamon()
  {
    System.Threading.Thread.Sleep(250);
  }

  private AtomPubClient EnsureInitAtomClient()
  {
    if (atom != null)
      return atom;

    atom = new AtomPubClient {
      Credential = new NetworkCredential(credential.HatenaId, credential.ApiKey),
      UserAgent = userAgent,
    };

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

    blogTitle = serviceDocument
      .Root
      .Element(AtomPub.ElementNames.AppWorkspace)
      ?.Element(AtomPub.ElementNames.AtomTitle)
      ?.Value;

    collectionUri = serviceDocument
      .Root
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
        throw new WebException($"エントリの取得に失敗したため中断しました ({statusCode})", WebExceptionStatus.ProtocolError);

      foreach (var entry in ReadEntries(collectionDocument)) {
        yield return entry;
      }

      // 次のatom:linkを取得する
      nextUri = collectionDocument
        .Element(AtomPub.Namespaces.Atom + "feed")
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

      if (!DateTimeOffset.TryParse(entry.Element(AtomPub.Namespaces.Atom + "published")?.Value, out var datePublished))
        datePublished = DateTimeOffset.MinValue;

      var e = new PostedEntry(
        id: id,
        memberUri: memberUri,
        entryUri: entryUri,
        datePublished: datePublished,
        authors: authors,
        formattedContent: formattedContent
      ) {
        /*
         * basic propeties
         */
        Title = entry.Element(AtomPub.Namespaces.Atom + "title")?.Value,
        Summary = entry.Element(AtomPub.Namespaces.Atom + "summary")?.Value,
        Content = entry.Element(AtomPub.Namespaces.Atom + "content")?.Value,
        ContentType = entry.Element(AtomPub.Namespaces.Atom + "content")?.GetAttributeValue("type"),
      };

      if (DateTimeOffset.TryParse(entry.Element(AtomPub.Namespaces.Atom + "updated")?.Value, out var dateUpdated))
        e.DateUpdated = dateUpdated;

      foreach (var category in entry.Elements(AtomPub.Namespaces.Atom + "category").Select(c => c.GetAttributeValue("term"))) {
        e.Categories.Add(category);
      }

      e.IsDraft = IsYes(entry.Element(AtomPub.Namespaces.App + "control")?.Element(AtomPub.Namespaces.App + "draft")?.Value);

      return e;
    }

    bool IsYes(string str) => string.Equals(str, "yes", StringComparison.OrdinalIgnoreCase);
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

    return new XDocument(
      new XDeclaration("1.0", "utf-8", null),
      entry
    );
  }
}
