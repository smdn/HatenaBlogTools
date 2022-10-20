// SPDX-FileCopyrightText: 2013 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;

namespace Smdn.HatenaBlogTools.HatenaBlog;

public class PostedEntry : Entry {
  /* read-only properties */
  public Uri Id { get; }
  public Uri MemberUri { get; }
  public Uri EntryUri { get; }
  public DateTimeOffset DatePublished { get; }
  public string FormattedContent { get; }

  /* read-write properties */
  public HashSet<string> Authors { get; }

  protected internal PostedEntry(
    Uri id,
    Uri memberUri,
    Uri entryUri,
    DateTimeOffset datePublished,
    IEnumerable<string> authors,
    string formattedContent,
    IEnumerable<string> categories = null
  )
    : base(
      categories: categories
    )
  {
    Id = id;
    MemberUri = memberUri;
    EntryUri = entryUri;
    DatePublished = datePublished;
    FormattedContent = formattedContent;
    Authors = new HashSet<string>(authors ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
  }
}
