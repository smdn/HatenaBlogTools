// SPDX-FileCopyrightText: 2013 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Net;
using System.Xml.Linq;

namespace Smdn.HatenaBlogTools.HatenaBlog;

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
  public override string? UserAgent { get; set; }

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
