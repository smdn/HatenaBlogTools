// SPDX-FileCopyrightText: 2013 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;

namespace Smdn.HatenaBlogTools.HatenaBlog;

public class Entry {
  public string? Title { get; set; }
  public HashSet<string> Categories { get; }
  public DateTimeOffset? DateUpdated { get; set; }
  public bool IsDraft { get; set; }
  public string? Summary { get; set; }
  public string? Content { get; set; }
  public string? ContentType { get; set; }

  // default constructor
  public Entry()
  {
    Categories = new HashSet<string>(StringComparer.Ordinal);
  }

  // copy constructor
  public Entry(Entry baseEntry)
    : this(
      title: baseEntry == null ? throw new ArgumentNullException(nameof(baseEntry)) : baseEntry.Title,
      categories: baseEntry.Categories,
      dateUpdated: baseEntry.DateUpdated,
      isDraft: baseEntry.IsDraft,
      summary: baseEntry.Summary,
      content: baseEntry.Content,
      contentType: baseEntry.ContentType
    )
  {
  }

  public Entry(
    string? title = null,
    IEnumerable<string>? categories = null,
    DateTimeOffset? dateUpdated = null,
    bool isDraft = false,
    string? summary = null,
    string? content = null,
    string? contentType = null
  )
  {
    Title = title;
    Categories = new HashSet<string>(categories ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
    DateUpdated = dateUpdated;
    IsDraft = isDraft;
    Summary = summary;
    Content = content;
    ContentType = contentType;
  }
}
