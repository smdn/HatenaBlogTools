// SPDX-FileCopyrightText: 2013 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
using System.Diagnostics.CodeAnalysis;
#endif
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;

using Smdn.HatenaBlogTools.AtomPublishingProtocol;
using Smdn.Xml.Linq;

namespace Smdn.HatenaBlogTools.HatenaBlog;

internal class DefaultHatenaBlogAtomPubClient : HatenaBlogAtomPubClient {
  private static Uri? ToUriNullable(string? val) => val is null ? null : new Uri(val);
  private static Exception CreateNotLoggedIn() => new InvalidOperationException($"not logged in yet. call {nameof(Login)}() first.");

  private readonly HatenaBlogAtomPubCredential credential;

  public override string HatenaId => credential.HatenaId;
  public override string BlogId => credential.BlogId;

  private readonly Uri rootEndPoint;
  public override Uri RootEndPoint => rootEndPoint;

  private string? blogTitle;
  public override string BlogTitle => blogTitle ?? throw CreateNotLoggedIn();

  private Uri? collectionUri;
  public override Uri CollectionUri => collectionUri ?? throw CreateNotLoggedIn();

  private string? userAgent;
  public override string? UserAgent {
    get => atom?.UserAgent ?? userAgent;
    set {
      userAgent = value;

      if (atom != null)
        atom.UserAgent = value;
    }
  }

  private AtomPubClient? atom = null;

#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
  [MemberNotNull(nameof(atom))]
#endif
  private void ThrowIfNotLoggedIn()
  {
    if (atom is null)
      throw CreateNotLoggedIn();
  }

  private static Uri GetRootEndPont(string hatenaId, string blogId)
    => new(string.Concat("https://blog.hatena.ne.jp/", hatenaId, "/", blogId, "/atom"));

  internal DefaultHatenaBlogAtomPubClient(HatenaBlogAtomPubCredential credential)
  {
    this.credential = credential ?? throw new ArgumentNullException(nameof(credential));
    rootEndPoint = GetRootEndPont(credential.HatenaId, credential.BlogId);
  }

  public override void WaitForCinnamon()
    => System.Threading.Thread.Sleep(250);

  private AtomPubClient EnsureInitAtomClient()
  {
    atom ??= new AtomPubClient(
      credential: new NetworkCredential(credential.HatenaId, credential.ApiKey),
      userAgent: userAgent
    );

    return atom;
  }

  public override HttpStatusCode Login(out XDocument? serviceDocument)
    => GetServiceDocuments(out serviceDocument);

  private HttpStatusCode GetServiceDocuments(out XDocument? serviceDocument)
  {
    var statusCode = EnsureInitAtomClient().Get(RootEndPoint, out serviceDocument);

    if (statusCode != HttpStatusCode.OK)
      return statusCode;
    if (serviceDocument is null)
      throw new InvalidOperationException("could not get response XML document");
    if (serviceDocument.Root is null)
      throw new InvalidOperationException("could not read response XML document (empty document)");
    if (serviceDocument.Root.Name != AtomPub.ElementNames.AppService)
      throw new NotSupportedException($"unexpected document type: {serviceDocument.Root.Name}");

    blogTitle = serviceDocument
      .Root
      .Element(AtomPub.ElementNames.AppWorkspace)
      ?.Element(AtomPub.ElementNames.AtomTitle)
      ?.Value
      ?? throw new InvalidOperationException("could not get blog title");

    collectionUri = serviceDocument
      .Root
      .Element(AtomPub.ElementNames.AppWorkspace)
      ?.Elements(AtomPub.ElementNames.AppCollection)
      ?.FirstOrDefault(static e => e.Element(AtomPub.ElementNames.AppAccept)?.Value?.Contains("type=entry") ?? false)
      ?.GetAttributeValue("href", static val => new Uri(val))
      ?? throw new InvalidOperationException("could not get blog collection URI");

    return statusCode;
  }

  protected override IEnumerable<Tuple<PostedEntry, XElement>> EnumerateAllEntries()
  {
    ThrowIfNotLoggedIn();

    var nextUri = CollectionUri;

    for (; ; ) {
#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
#pragma warning disable CS8602
#endif
      var statusCode = atom.Get(nextUri, out var collectionDocument);
#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
#pragma warning restore CS8602
#endif

      if (statusCode != HttpStatusCode.OK || collectionDocument is null)
        throw new WebException($"エントリの取得に失敗したため中断しました ({statusCode})", WebExceptionStatus.ProtocolError);

      foreach (var entry in ReadEntries(collectionDocument)) {
        yield return entry;
      }

      // 次のatom:linkを取得する
      nextUri = collectionDocument
        .Element(AtomPub.Namespaces.Atom + "feed")
        ?.Elements(AtomPub.Namespaces.Atom + "link")
        ?.FirstOrDefault(static e => e.HasAttributeWithValue("rel", "next"))
        ?.GetAttributeValue("href", ToUriNullable);

      if (nextUri == null)
        break;

      WaitForCinnamon();
    }
  }

  internal static IEnumerable<Tuple<PostedEntry, XElement>> ReadEntries(XDocument doc)
  {
    return doc
      .Element(AtomPub.Namespaces.Atom + "feed")
      ?.Elements(AtomPub.Namespaces.Atom + "entry")
      ?.Select(static entry => Tuple.Create(ConvertEntry(entry), entry))
      ?? Enumerable.Empty<Tuple<PostedEntry, XElement>>();

    static PostedEntry ConvertEntry(XElement entry)
    {
      /*
       * posted-entry only propeties
       */
      var memberUri = entry
        .Elements(AtomPub.Namespaces.Atom + "link")
        .FirstOrDefault(static link => link.HasAttributeWithValue("rel", "edit"))
        ?.GetAttributeValue("href", ToUriNullable);
      var entryUri = entry
        .Elements(AtomPub.Namespaces.Atom + "link")
        .FirstOrDefault(
          static link => link.HasAttributeWithValue("rel", "alternate") && link.HasAttributeWithValue("type", "text/html")
        )
        ?.GetAttributeValue("href", ToUriNullable);
      var id = ToUriNullable(entry.Element(AtomPub.Namespaces.Atom + "id")?.Value);
      var formattedContent = entry.Element(AtomPub.Namespaces.Hatena + "formatted-content")?.Value;
      var authors = entry
        .Elements(AtomPub.Namespaces.Atom + "author")
        .Select(static elementAuthor => elementAuthor.Element(AtomPub.Namespaces.Atom + "name"))
        .Where(static name => name is not null && name.Value is not null)
        .Select(static author => author!.Value!);

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

      e.Categories.UnionWith(
        entry
          .Elements(AtomPub.Namespaces.Atom + "category")
          .Select(static c => c.GetAttributeValue("term"))
      );

      e.IsDraft = IsYes(
        entry.Element(AtomPub.Namespaces.App + "control")
        ?.Element(AtomPub.Namespaces.App + "draft")
        ?.Value
      );

      return e;
    }

    static bool IsYes(string? str)
      => string.Equals(str, "yes", StringComparison.OrdinalIgnoreCase);
  }

  public override HttpStatusCode UpdateEntry(PostedEntry updatingEntry, out XDocument? responseDocument)
  {
    ThrowIfNotLoggedIn();

    if (updatingEntry.MemberUri is null)
      throw new InvalidOperationException($"cannot edit this entry since member URI is not set (ID: {updatingEntry.Id}, entry URI: {updatingEntry.EntryUri}, title: {updatingEntry.Title})");

    responseDocument = null;

    try {
      var putDocument = CreatePostDocument(updatingEntry);

      putDocument.Root!.Add(
        updatingEntry.Authors.Select(static author =>
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

#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
#pragma warning disable CS8602
#endif
      return atom.Put(updatingEntry.MemberUri, putDocument, out responseDocument);
#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
#pragma warning restore CS8602
#endif
    }
    catch (Exception ex) {
      throw new PostEntryFailedException(updatingEntry, ex);
    }
  }

  public override HttpStatusCode PostEntry(Entry entry, out XDocument? responseDocument)
  {
    ThrowIfNotLoggedIn();

    try {
#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
#pragma warning disable CS8602
#endif
      return atom.Post(CollectionUri, CreatePostDocument(entry), out responseDocument);
#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
#pragma warning restore CS8602
#endif
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
        postEntry.Title is null ? null : new XText(postEntry.Title)
      ),
      postEntry.DateUpdated.HasValue
        ? new XElement(
          AtomPub.Namespaces.Atom + "updated",
          new XText(
            XmlConvert.ToString(postEntry.DateUpdated.Value)
          )
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
        postEntry.Content is null ? null : new XText(postEntry.Content)
      ),
      postEntry.Categories.Select(
        static c => new XElement(
          AtomPub.Namespaces.Atom + "category",
          new XAttribute("term", c)
        )
      ),
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
