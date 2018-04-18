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
using System.Net;

namespace Smdn.Applications.HatenaBlogTools {
  public interface IHatenaBlogEntryEditor {
    void Edit(PostedEntry entry, Action<string, string> actionIfModified);
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
                                           IDiffGenerator diff)
    {
      foreach (var entry in hatenaBlog.EnumerateEntries()) {
        EditEntryContent(hatenaBlog,
                         entry,
                         postMode,
                         editor,
                         diff);

        hatenaBlog.WaitForCinnamon();
      }
    }

    public static void EditEntryContent(HatenaBlogAtomPubClient hatenaBlog,
                                        PostedEntry entry,
                                        PostMode postMode,
                                        IHatenaBlogEntryEditor editor,
                                        IDiffGenerator diff)
    {
      Console.Write("{0} \"{1}\" ", entry.EntryUri, entry.Title);

      var modified = false;

      editor.Edit(entry, (originalText, modifiedText) => {
        Console.WriteLine();

        diff.DisplayDifference(originalText, modifiedText);

        modified = true;
      });

      if (!modified)
        Console.WriteLine("(変更なし)");

      switch (postMode) {
        case PostMode.PostNever:
          return;

        case PostMode.PostIfModified:
          if (modified)
            break;
          else
            return;

        case PostMode.PostAlways:
          break;

        default:
          throw new ArgumentException($"invalid mode: {postMode}", nameof(postMode));
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
    }
  }
}