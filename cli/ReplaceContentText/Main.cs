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
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Smdn.Applications.HatenaBlogTools.HatenaBlog;

namespace Smdn.Applications.HatenaBlogTools {
  partial class ReplaceContentText : CliBase {
    protected override string GetDescription() => "すべての記事に対して置換を行います。";

    protected override string GetUsageExtraMandatoryOptions() => "--from <置換前の文字列> [--to <置換後の文字列>]";

    protected override IEnumerable<string> GetUsageExtraOptionDescriptions()
    {
      yield return "--from <置換前の文字列>    : 置換したい文字列を指定します";
      yield return "--to <置換後の文字列>      : 置換後の文字列を指定します";
      yield return "--regex                    : --fromおよび--toで指定された文字列を正規表現として解釈します";
      yield return "-n, --dry-run              : 置換結果の確認だけ行い、再投稿を行いません";
      yield return "-i, --interactive          : 置換結果の再投稿を行う前に確認を行います";
      yield return "";
      yield return "変更箇所の表示に関するオプション:";
      yield return "  --diff-cmd <コマンド>           : 再投稿前に指定された<コマンド>を使って変更箇所を表示します";
      yield return "  --diff-cmd-args <コマンド引数>  : --diff-cmdで指定されたコマンドに渡す引数(オプション)を指定します";
      yield return "  --diff-test                     : --diff-cmdで指定されたコマンドの動作テストを行います";
      yield return "                                    このオプションを指定した場合、はてなブログの記事の更新は一切行いません";
    }

    public void Run(string[] args)
    {
      if (!ParseCommonCommandLineArgs(ref args,
                                      new[] {"-diff-test"},
                                      out HatenaBlogAtomPubCredential credential)) {
        return;
      }

      string replaceFromText = null;
      string replaceToText = null;
      bool replaceAsRegex = false;
      string diffCommand = null;
      string diffCommandArgs = null;
      bool testDiffCommand = false;
      bool dryrun = false;
      bool confirm = false;

      for (var i = 0; i < args.Length; i++) {
        switch (args[i]) {
          case "--from":
          case "-from":
            replaceFromText = args[++i];
            break;

          case "--to":
          case "-to":
            replaceToText = args[++i];
            break;

          case "--regex":
          case "-regex":
            replaceAsRegex = true;
            break;

          case "--diff-cmd":
            diffCommand = args[++i];
            break;

          case "--diff-cmd-args":
            diffCommandArgs = args[++i];
            break;

          case "--diff-test":
            testDiffCommand = true;
            break;

          case "--dry-run":
          case "-n":
            dryrun = true;
            break;

          case "--interactive":
          case "-i":
            confirm = true;
            break;
        }
      }

      var diffGenerator = DiffGenerator.Create(false,
                                               diffCommand,
                                               diffCommandArgs,
                                               "置換前の本文",
                                               "置換後の本文");

      if (testDiffCommand) {
        DiffGenerator.Test(diffGenerator);
        return;
      }

      if (string.IsNullOrEmpty(replaceFromText)) {
        Usage("置換する文字列を指定してください");
        return;
      }

      if (replaceToText == null)
        replaceToText = string.Empty; // delete

      var editor = replaceAsRegex
        ? (IHatenaBlogEntryEditor)new RegexEntryEditor(replaceFromText, replaceToText)
        : (IHatenaBlogEntryEditor)new EntryEditor(replaceFromText, replaceToText);

      var postMode = dryrun
        ? HatenaBlogFunctions.PostMode.PostNever
        : HatenaBlogFunctions.PostMode.PostIfModified;

      Func<bool> confirmBeforePosting = null;

      if (confirm)
        confirmBeforePosting = () => ConsoleUtils.AskYesNo(false, "更新しますか");

      if (!Login(credential, out HatenaBlogAtomPubClient hatenaBlog))
        return;

      HatenaBlogFunctions.EditAllEntryContent(hatenaBlog,
                                              postMode,
                                              editor,
                                              diffGenerator,
                                              null,
                                              confirmBeforePosting,
                                              out _,
                                              out _);
    }

    private class EntryEditor : IHatenaBlogEntryEditor {
      private readonly string replaceFrom;
      private readonly string replaceTo;

      public EntryEditor(string replaceFrom, string replaceTo)
      {
        this.replaceFrom = replaceFrom;
        this.replaceTo = replaceTo;
      }

      public bool Edit(PostedEntry entry, out string originalText, out string modifiedText)
      {
        originalText = entry.Content;
        modifiedText = originalText.Replace(replaceFrom, replaceTo);

        if (originalText.Length == modifiedText.Length &&
            string.Equals(originalText, modifiedText, StringComparison.Ordinal)) {
          // not modified
          return false;
        }
        else {
          entry.Content = modifiedText;

          return true;
        }
      }
    }

    private class RegexEntryEditor : IHatenaBlogEntryEditor {
      private readonly Regex regexToReplace;
      private readonly string replacement;

      public RegexEntryEditor(string regexToReplace, string replacement)
      {
        this.regexToReplace = new Regex(regexToReplace, RegexOptions.Multiline);
        this.replacement = replacement;
      }

      public bool Edit(PostedEntry entry, out string originalText, out string modifiedText)
      {
        var modified = false;

        originalText = entry.Content;

        modifiedText = regexToReplace.Replace(originalText, (match) => {
          modified |= true;

          return match.Result(replacement);
        });

        if (modified)
          entry.Content = modifiedText;

        return modified;
      }
    }
  }
}
