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

namespace Smdn.Applications.HatenaBlogTools {
  partial class MainClass {
    private static string GetUsageExtraMandatoryOptions() => ""; // TODO

    private static IEnumerable<string> GetUsageExtraOptionDescriptions()
    {
      // TODO
      yield break;
    }

    public static void Main(string[] args)
    {
      HatenaBlogAtomPubClient.InitializeHttpsServicePoint();

      if (!ParseCommonCommandLineArgs(ref args, out HatenaBlogAtomPubClient hatenaBlog))
        return;

      string diffCommand = null;
      string diffCommandArgs = null;
      bool dryrun = false;

      for (var i = 0; i < args.Length; i++) {
        switch (args[i]) {
          case "-diff-cmd":
            diffCommand = args[++i];
            break;

          case "-diff-cmd-args":
            diffCommandArgs = args[++i];
            break;

          case "-n":
            dryrun = true;
            break;
        }
      }

      var editor = new EntryEditor(blogDomain: hatenaBlog.BlogId,
                                   customBlogDomain: null, // TODO
                                   fixMixedContent: false, // TODO
                                   replaceBlogUrl: false); // TODO

      var diffGenerator = DiffGenerator.Create(false,
                                               diffCommand,
                                               diffCommandArgs,
                                               "変更前の本文",
                                               "変更後の本文");

      if (!diffGenerator.IsAvailable()) {
        Usage("指定されたdiffコマンドは利用できません。　コマンドのパスと、カレントディレクトリに書き込みができることを確認してください。");
        return;
      }

      var postMode = dryrun
        ? HatenaBlogFunctions.PostMode.PostNever
        : HatenaBlogFunctions.PostMode.PostIfModified;

      if (!Login(hatenaBlog))
        return;

      HatenaBlogFunctions.EditAllEntryContent(hatenaBlog,
                                              postMode,
                                              editor,
                                              diffGenerator);
    }

    private class EntryEditor : IHatenaBlogEntryEditor {
      private readonly bool fixMixedContent;
      private readonly bool replaceBlogUrl;
      private readonly string[] blogDomains;

      public EntryEditor(string blogDomain, string customBlogDomain, bool fixMixedContent, bool replaceBlogUrl)
      {
        if (customBlogDomain == null)
          this.blogDomains = new[] { blogDomain };
        else
          this.blogDomains = new[] { blogDomain, customBlogDomain };

        this.fixMixedContent = fixMixedContent;
        this.replaceBlogUrl = replaceBlogUrl;
      }

      public bool Edit(PostedEntry entry, out string originalText, out string modifiedText)
      {
        originalText = entry.Content;
        modifiedText = null;

        var contentEditor = new HatenaBlogContentEditor(originalText);
        var modified = false;

        if (fixMixedContent)
          modified |= contentEditor.FixMixedContentReferences();

        if (replaceBlogUrl)
          modified |= contentEditor.ReplaceBlogUrlToHttps(blogDomains);

        if (modified) {
          entry.Content = contentEditor.ToString();

          modifiedText = entry.Content;
        }

        return modified;
      }
    }
  }
}
