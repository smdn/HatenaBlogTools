//
// Author:
//       smdn <smdn@smdn.jp>
//
// Copyright (c) 2020 smdn
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

using Smdn.Applications.HatenaBlogTools.HatenaBlog;

namespace Smdn.Applications.HatenaBlogTools {
  abstract class EntryTextModifier {
    public abstract bool Modify(
      PostedEntry entry,
      Converter<string, string> modifier,
      out string originalText,
      out string modifiedText
    );

    protected bool Modify(
      string input,
      Converter<string, string> modifier,
      out string original,
      out string modified
    )
    {
      original = input;
      modified = modifier(input);

      return !(
        original.Length == modified.Length &&
        string.Equals(original, modified, StringComparison.Ordinal)
      );
    }
  }

  class EntryTitleModifier : EntryTextModifier {
    public override bool Modify(
      PostedEntry entry,
      Converter<string, string> modifier,
      out string originalText,
      out string modifiedText
    )
    {
      if (Modify(entry.Title, modifier, out originalText, out modifiedText)) {
        entry.Title = modifiedText;
        return true;
      }

      return false;
    }
  }

  class EntryContentModifier : EntryTextModifier {
    public override bool Modify(
      PostedEntry entry,
      Converter<string, string> modifier,
      out string originalText,
      out string modifiedText
    )
    {
      if (Modify(entry.Content, modifier, out originalText, out modifiedText)) {
        entry.Content = modifiedText;
        return true;
      }

      return false;
    }
  }
}
