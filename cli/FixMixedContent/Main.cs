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
using System.IO;
using System.Linq;
using System.Text;

using Smdn.Applications.HatenaBlogTools.HatenaBlog;

namespace Smdn.Applications.HatenaBlogTools {
  partial class FixMixedContent : CliBase {
    protected override string GetDescription() => "ブログ記事の混在コンテンツとなりうるURL、および自ブログのリンクURLを修正します。";

    protected override string GetUsageExtraMandatoryOptions() => "[--fix-mixed-content] [--fix-blog-url] [--update-content]";

    protected override IEnumerable<string> GetUsageExtraOptionDescriptions()
    {
      yield return "--fix-mixed-content         : 混在コンテンツとなりうるリンクURLをhttpからhttpsに修正します";
      yield return "--fix-blog-url              : 自ブログのリンクURLをhttpからhttpsに修正します";
      yield return "--custom-domain <ドメイン>  : 独自ドメインを使用している場合は、その独自ドメイン名を指定します";
      yield return "                              --fix-blog-url で独自ドメインのURLも修正する場合に指定します";
      yield return "--update-content            : 変更箇所がない場合でも常に再投稿して記事を更新します";
      yield return "                              ブログをHTTPS化している場合は、再投稿によりはてな記法等で埋め込まれたURLが更新されます";
      yield return "--list-fixed-entry          : すべての記事の更新を行ったあとに、再投稿した記事のURL一覧を表示します";
      yield return "";
      yield return "-n, --dry-run               : 修正した内容の確認だけ行い、再投稿を行いません";
      yield return "-i, --interactive           : 修正した内容の再投稿を行う前に確認を行います";
      yield return "";

      yield return "変更箇所の表示に関するオプション:";
      yield return "  --diff-cmd <コマンド>           : 再投稿前に指定された<コマンド>を使って変更箇所を表示します";
      yield return "  --diff-cmd-args <コマンド引数>  : --diff-cmdで指定されたコマンドに渡す引数(オプション)を指定します";
      yield return "  --diff-test                     : --diff-cmdで指定されたコマンドの動作テストを行います";
      yield return "                                    このオプションを指定した場合、はてなブログの記事の更新は一切行いません";
      yield return "";

      yield return "更新するコンテンツの指定に関するオプション:";
      yield return "  --input-content [ファイル名|-]  : はてなブログの記事を取得する代わりに、指定されたファイルの内容に対して修正します";
      yield return "                                    - を指定した場合は標準入力から読み込みます";
      yield return "                                    このオプションを指定した場合、はてなブログの記事の更新は一切行いません";
      yield return "  --output-content [ファイル名|-] : --input-contentで指定されたファイルの内容を修正した結果をファイルに出力します";
      yield return "                                    - を指定した場合は標準出力に書き込みます";
    }

    public void Run(string[] args)
    {
      if (!ParseCommonCommandLineArgs(ref args,
                                      new[] { "--diff-test", "--input-content", "--output-content" },
                                      out HatenaBlogAtomPubCredential credential)) {
        return;
      }

      bool postAlways = false;
      bool fixMixedContent = false;
      bool fixBlogUrl = false;
      string customBlogDomain = null;
      string diffCommand = null;
      string diffCommandArgs = null;
      bool testDiffCommand = false;
      string contentInput = null;
      string contentOutput = null;
      bool editLocalContent = false;
      bool dryRun = false;
      bool confirm = false;
      bool listFixedEntries = false;

      for (var i = 0; i < args.Length; i++) {
        switch (args[i]) {
          case "--update-content":
            postAlways = true;
            break;

          case "--fix-mixed-content":
            fixMixedContent = true;
            break;

          case "--fix-blog-url":
            fixBlogUrl = true;
            break;

          case "--custom-domain":
            customBlogDomain = args[++i];
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

          case "--input-content":
            contentInput = args[++i];
            editLocalContent = true;
            break;

          case "--output-content":
            contentOutput = args[++i];
            editLocalContent = true;
            break;

          case "--dry-run":
          case "-n":
            dryRun = true;
            break;

          case "--interactive":
          case "-i":
            confirm = true;
            break;

          case "--list-fixed-entry":
            listFixedEntries = true;
            break;
        }
      }

      var diffGenerator = DiffGenerator.Create(false,
                                               diffCommand,
                                               diffCommandArgs,
                                               "変更前の本文",
                                               "変更後の本文");

      if (testDiffCommand) {
        DiffGenerator.Test(diffGenerator);
        return;
      }

      if (editLocalContent && string.IsNullOrEmpty(customBlogDomain)) {
        Usage("--custom-domainを指定してください");
        return;
      }

      var editor = new EntryEditor(blogDomain: credential?.BlogId,
                                   customBlogDomain: customBlogDomain,
                                   fixMixedContent: fixMixedContent,
                                   replaceBlogUrl: fixBlogUrl);

      if (editLocalContent) {
        EditContent(editor, contentInput, contentOutput);
        return;
      }

      var postMode = HatenaBlogFunctions.PostMode.PostIfModified;

      if (postAlways)
        postMode = HatenaBlogFunctions.PostMode.PostAlways;

      if (dryRun)
        postMode = HatenaBlogFunctions.PostMode.PostNever;

      Func<bool> confirmBeforePosting = null;

      if (confirm)
        confirmBeforePosting = () => ConsoleUtils.AskYesNo(false, "更新しますか");

      if (!Login(credential, out HatenaBlogAtomPubClient hatenaBlog))
        return;

      HatenaBlogFunctions.EditAllEntryContent(hatenaBlog,
                                              postMode,
                                              editor,
                                              diffGenerator,
                                              confirmBeforePosting,
                                              out IReadOnlyList<PostedEntry> updatedEntries,
                                              out IReadOnlyList<PostedEntry> modifiedEntries);

      if (listFixedEntries) {
        Console.WriteLine();
        Console.WriteLine("下記エントリに対して修正を行い再投稿しました。");

        foreach (var modifiedEntry in modifiedEntries) {
          Console.WriteLine("{0} \"{1}\"", modifiedEntry.EntryUri, modifiedEntry.Title);
        }
      }

      Console.WriteLine("完了");
    }

    private static void EditContent(IHatenaBlogEntryEditor editor, string input, string output)
    {
      bool IsStdIO(string io) {
        if (string.IsNullOrEmpty(io))
          return true;
        if (string.Equals(io, "-", StringComparison.Ordinal))
          return true;

        return false;
      }

      var encoding = new UTF8Encoding(false);

      using (var inputStream = IsStdIO(input) ? Console.OpenStandardInput() : File.OpenRead(input)) {
        var reader = new StreamReader(inputStream, false);
        var modified = editor.Edit(new PostedEntry() { Content = reader.ReadToEnd() },
                                   out string originalText,
                                   out string modifiedText);

        using (var outputStream = IsStdIO(output) ? Console.OpenStandardOutput() : File.Create(output)) {
          var writer = new StreamWriter(outputStream, encoding);

          if (modified)
            writer.Write(modifiedText);
          else
            writer.Write(originalText);

          writer.Flush();
        }
      }
    }

    private class EntryEditor : IHatenaBlogEntryEditor {
      private readonly bool fixMixedContent;
      private readonly bool replaceBlogUrl;
      private readonly string[] blogDomains;

      public EntryEditor(string blogDomain, string customBlogDomain, bool fixMixedContent, bool replaceBlogUrl)
      {
        if (replaceBlogUrl && blogDomain == null && customBlogDomain == null)
          throw new ArgumentException($"{nameof(blogDomain)} or {nameof(customBlogDomain)} must be specified");

        this.blogDomains = (new[] { blogDomain, customBlogDomain }).Where(d => d != null).ToArray();

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
