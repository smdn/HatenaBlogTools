//
// Author:
//       smdn <smdn@smdn.jp>
//
// Copyright (c) 2014 smdn
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
// THE SOFTWARE.using System;

using System;
using System.Text.RegularExpressions;

using Smdn.Applications.HatenaBlogTools.HatenaBlog;

namespace Smdn.Applications.HatenaBlogTools {
  class EntryEditor : IHatenaBlogEntryEditor {
    private readonly string replaceFrom;
    private readonly string replaceTo;
    private readonly EntryTextModifier entryTextModifier;

    public EntryEditor(string replaceFrom, string replaceTo, EntryTextModifier entryTextModifier)
    {
      this.replaceFrom = replaceFrom;
      this.replaceTo = replaceTo;
      this.entryTextModifier = entryTextModifier ?? throw new ArgumentNullException(nameof(entryTextModifier));
    }

    public bool Edit(Entry entry, out string originalText, out string modifiedText)
    {
      return entryTextModifier.Modify(
        entry: entry,
        modifier: original => original.Replace(replaceFrom, replaceTo),
        out originalText,
        out modifiedText
      );
    }
  }

  class RegexEntryEditor : IHatenaBlogEntryEditor {
    private readonly Regex regexToReplace;
    private readonly string replacement;
    private readonly EntryTextModifier entryTextModifier;

    public RegexEntryEditor(string regexToReplace, string replacement, EntryTextModifier entryTextModifier)
    {
      this.regexToReplace = new Regex(regexToReplace, RegexOptions.Multiline);
      this.replacement = replacement;
      this.entryTextModifier = entryTextModifier ?? throw new ArgumentNullException(nameof(entryTextModifier));
    }

    public bool Edit(Entry entry, out string originalText, out string modifiedText)
    {
      return entryTextModifier.Modify(
        entry: entry,
        modifier: original => regexToReplace.Replace(original, match => match.Result(replacement)),
        out originalText,
        out modifiedText
      );
    }
  }
}
