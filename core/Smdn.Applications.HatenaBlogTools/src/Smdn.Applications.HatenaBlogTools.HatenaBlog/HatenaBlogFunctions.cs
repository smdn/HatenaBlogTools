//
// Author:
//       smdn <smdn@smdn.jp>
//
// Copyright (c) 2018 smdn
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
using System.Net;

namespace Smdn.Applications.HatenaBlogTools.HatenaBlog {
  public interface IHatenaBlogEntryEditor {
    bool Edit(PostedEntry entry, out string originalText, out string modifiedText);
  }

  public static class HatenaBlogFunctions {
    public enum PostMode {
      PostNever,
      PostAlways,
      PostIfModified,
    }

    public static void EditAllEntryContent(HatenaBlogAtomPubClient hatenaBlog,
                                           PostMode postMode,
                                           IHatenaBlogEntryEditor editor,
                                           IDiffGenerator diff,
                                           Func<bool> confirmBeforePosting,
                                           out IReadOnlyList<PostedEntry> updatedEntries,
                                           out IReadOnlyList<PostedEntry> modifiedEntries)
    {
      var _updatedEntries = new List<PostedEntry>();
      var _modifiedEntries = new List<PostedEntry>();

      updatedEntries = _updatedEntries;
      modifiedEntries = _modifiedEntries;

      foreach (var entry in hatenaBlog.EnumerateEntries()) {
        var statusCode = EditEntryContent(hatenaBlog,
                                          entry,
                                          postMode,
                                          editor,
                                          diff,
                                          confirmBeforePosting,
                                          out bool modified);

        if (statusCode == HttpStatusCode.OK) {
          _updatedEntries.Add(entry);

          if (modified)
            _modifiedEntries.Add(entry);
        }

        hatenaBlog.WaitForCinnamon();
      }
    }

    public static HttpStatusCode EditEntryContent(HatenaBlogAtomPubClient hatenaBlog,
                                                  PostedEntry entry,
                                                  PostMode postMode,
                                                  IHatenaBlogEntryEditor editor,
                                                  IDiffGenerator diff,
                                                  Func<bool> confirmBeforePosting,
                                                  out bool modified)
    {
      Console.Write("{0} \"{1}\" ", entry.EntryUri, entry.Title);

      modified = false;

      if (editor.Edit(entry, out string originalText, out string modifiedText)) {
        modified = true;

        Console.WriteLine();

        diff.DisplayDifference(originalText, modifiedText);
      }
      else {
        Console.WriteLine("(変更なし)");
      }

      switch (postMode) {
        case PostMode.PostNever:
          return (HttpStatusCode)0;

        case PostMode.PostIfModified:
          if (modified)
            break;
          else
            return (HttpStatusCode)0;

        case PostMode.PostAlways:
          break;

        default:
          throw new ArgumentException($"invalid mode: {postMode}", nameof(postMode));
      }

      if (confirmBeforePosting != null && !confirmBeforePosting()) {
        Console.WriteLine("更新しません");
        return (HttpStatusCode)0;
      }

      Console.Write("{0} \"{1}\" を更新中 ... ", entry.EntryUri, entry.Title);

      var statusCode = hatenaBlog.UpdateEntry(entry, out _);

      if (statusCode == HttpStatusCode.OK) {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("更新しました");
        Console.ResetColor();
      }
      else {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine("更新に失敗しました: {0}", statusCode);
        Console.ResetColor();
      }

      return statusCode;
    }
  }
}