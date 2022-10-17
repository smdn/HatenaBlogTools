//
// Author:
//       smdn <smdn@smdn.jp>
//
// Copyright (c) 2013 smdn
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

namespace Smdn.HatenaBlogTools.HatenaBlog {
  public class Entry {
    public string Title { get; set; }
    public HashSet<string> Categories { get; } = new HashSet<string>(StringComparer.Ordinal);
    public DateTimeOffset? DateUpdated { get; set; }
    public bool IsDraft { get; set; } = false;
    public string Summary { get; set; }
    public string Content { get; set; }
    public string ContentType { get; set; }

    // default constructor
    public Entry()
    {
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
      string title = null,
      IEnumerable<string> categories = null,
      DateTimeOffset? dateUpdated = null,
      bool isDraft = false,
      string summary = null,
      string content = null,
      string contentType = null
    )
    {
      this.Title = title;
      this.Categories = new HashSet<string>(categories ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
      this.DateUpdated = dateUpdated;
      this.IsDraft = isDraft;
      this.Summary = summary;
      this.Content = content;
      this.ContentType = contentType;
    }
  }
}
