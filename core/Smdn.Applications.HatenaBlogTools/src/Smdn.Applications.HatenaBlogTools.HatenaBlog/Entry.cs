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

namespace Smdn.Applications.HatenaBlogTools.HatenaBlog {
  public class Entry {
    public string Title;
    public HashSet<string> Categories = new HashSet<string>(StringComparer.Ordinal);
    public DateTimeOffset? Updated;
    public bool IsDraft;
    public string Summary;
    public string Content;
    public string ContentType;

    // default constructor
    public Entry()
    {
    }

    // copy constructor
    public Entry(Entry baseEntry)
    {
      if (baseEntry == null)
        throw new ArgumentNullException(nameof(baseEntry));

      this.Title = baseEntry.Title;
      this.Categories = new HashSet<string>(baseEntry.Categories, StringComparer.Ordinal);
      this.Updated = baseEntry.Updated;
      this.IsDraft = baseEntry.IsDraft;
      this.Summary = baseEntry.Summary;
      this.Content = baseEntry.Content;
      this.ContentType = baseEntry.ContentType;
    }
  }
}
