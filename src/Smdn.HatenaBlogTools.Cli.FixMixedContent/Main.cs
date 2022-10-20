// SPDX-FileCopyrightText: 2018 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Smdn.HatenaBlogTools.HatenaBlog;

namespace Smdn.HatenaBlogTools;

public partial class FixMixedContent : CliBase {
  protected override string GetDescription() => "ブログ記事の混在コンテンツとなりうるURL、および自ブログのリンクURLを修正します。";

  protected override string GetUsageExtraMandatoryOptions() => "[--fix-mixed-content] [--fix-blog-url] [--update-content]";

  protected override IEnumerable<string> GetUsageExtraOptionDescriptions()
  {
    yield return "--fix-mixed-content         : 混在コンテンツとなりうるリンクURLをhttpからhttpsに修正します";
    yield return "--include-domain <ドメイン> : --fix-mixed-contentで修正するURLのうち、指定された<ドメイン>のURLのみを修正対象とします";
    yield return "                              複数指定可、ただし--exclude-domainと同時に指定することはできません";
    yield return "--exclude-domain <ドメイン> : --fix-mixed-contentで修正するURLのうち、指定された<ドメイン>のURLを修正対象外とします";
    yield return "                              複数指定可、ただし--include-domainと同時に指定することはできません";
    yield return string.Empty;
    yield return "--fix-blog-url              : 自ブログのリンクURLをhttpからhttpsに修正します";
    yield return "--custom-domain <ドメイン>  : 独自ドメインを使用している場合は、その独自ドメイン名を指定します";
    yield return "                              --fix-blog-url で独自ドメインのURLも修正する場合に指定します";
    yield return string.Empty;
    yield return "--update-content            : 変更箇所がない場合でも常に再投稿して記事を更新します";
    yield return "                              ブログをHTTPS化している場合は、再投稿によりはてな記法等で埋め込まれたURLが更新されます";
    yield return string.Empty;
    yield return "--entry-url-skip-to <URL>   : 指定されたURLのエントリまで処理をスキップします";
    yield return "                              エラー等により中断した処理を途中から再開する場合などに指定してください";
    yield return string.Empty;
    yield return "--list-fixed-entry          : すべての記事の更新を行ったあとに、再投稿した記事のURL一覧を表示します";
    yield return string.Empty;
    yield return "-n, --dry-run               : 修正した内容の確認だけ行い、再投稿を行いません";
    yield return "-i, --interactive           : 修正した内容の再投稿を行う前に確認を行います";
    yield return string.Empty;

    yield return "変更箇所の表示に関するオプション:";
    yield return "  --diff-cmd <コマンド>           : 再投稿前に指定された<コマンド>を使って変更箇所を表示します";
    yield return "  --diff-cmd-args <コマンド引数>  : --diff-cmdで指定されたコマンドに渡す引数(オプション)を指定します";
    yield return "  --diff-test                     : --diff-cmdで指定されたコマンドの動作テストを行います";
    yield return "                                    このオプションを指定した場合、はてなブログの記事の更新は一切行いません";
    yield return string.Empty;

    yield return "更新するコンテンツの指定に関するオプション:";
    yield return "  --input-content [ファイル名|-]  : はてなブログの記事を取得する代わりに、指定されたファイルの内容に対して修正します";
    yield return "                                    - を指定した場合は標準入力から読み込みます";
    yield return "                                    このオプションを指定した場合、はてなブログの記事の更新は一切行いません";
    yield return "  --output-content [ファイル名|-] : --input-contentで指定されたファイルの内容を修正した結果をファイルに出力します";
    yield return "                                    - を指定した場合は標準出力に書き込みます";
  }

  public void Run(string[] args)
  {
    if (
      !ParseCommonCommandLineArgs(
        ref args,
        new[] { "--diff-test", "--input-content", "--output-content" },
        out var credential
      )
    ) {
      return;
    }

    bool postAlways = false;
    bool fixMixedContent = false;
    var fixMixedContentDomainsInclude = new HashSet<string>(StringComparer.Ordinal);
    var fixMixedContentDomainsExclude = new HashSet<string>(StringComparer.Ordinal);
    bool fixBlogUrl = false;
    string customBlogDomain = null;
    Uri entryUrlSkipTo = null;
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

        case "--include-domain":
          fixMixedContentDomainsInclude.Add(args[++i]);
          break;

        case "--exclude-domain":
          fixMixedContentDomainsExclude.Add(args[++i]);
          break;

        case "--fix-blog-url":
          fixBlogUrl = true;
          break;

        case "--custom-domain":
          customBlogDomain = args[++i];
          break;

        case "--entry-url-skip-to":
          entryUrlSkipTo = new Uri(args[++i]);
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

    var diffGenerator = DiffGenerator.Create(
      false,
      diffCommand,
      diffCommandArgs,
      "変更前の本文",
      "変更後の本文"
    );

    if (testDiffCommand) {
      DiffGenerator.Test(diffGenerator);
      return;
    }

    if (editLocalContent && fixBlogUrl && string.IsNullOrEmpty(customBlogDomain)) {
      Usage("--custom-domainを指定してください");
      return;
    }

    Predicate<Html.HtmlAttribute> predicateForFixMixedContent = null;

    if (fixMixedContent) {
      if (0 < fixMixedContentDomainsInclude.Count && 0 < fixMixedContentDomainsExclude.Count) {
        Usage("--exclude-domainと--include-domainを同時に指定することはできません");
        return;
      }
      else {
        var include = 0 < fixMixedContentDomainsInclude.Count;
        var domainList = include ? fixMixedContentDomainsInclude : fixMixedContentDomainsExclude;
        var domainPrefixList = domainList.Select(domain => "//" + domain + "/").ToList();

        predicateForFixMixedContent = attr => {
          foreach (var domainPrefix in domainPrefixList) {
            if (attr.Value.Contains(domainPrefix))
              return include;
          }

          return !include;
        };
      }
    }

    var editor = new EntryEditor(
      blogDomain: credential?.BlogId,
      customBlogDomain: customBlogDomain,
      fixMixedContent: fixMixedContent,
      predicateForFixMixedContent: predicateForFixMixedContent,
      replaceBlogUrl: fixBlogUrl
    );

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

    if (!Login(credential, out var hatenaBlog))
      return;

    IReadOnlyList<PostedEntry> updatedEntries = null;
    IReadOnlyList<PostedEntry> modifiedEntries = null;
    var success = true;

    try {
      HatenaBlogFunctions.EditAllEntry(
        hatenaBlog,
        postMode,
        editor,
        diffGenerator,
        entryUrlSkipTo,
        confirmBeforePosting,
        out updatedEntries,
        out modifiedEntries
      );
    }
    catch (PostEntryFailedException ex) {
      success = false;

      Console.Error.WriteLine(ex);

      Console.ForegroundColor = ConsoleColor.Red;

      if (ex.CausedEntry is PostedEntry entry)
        Console.WriteLine($"エントリの更新に失敗しました ({entry.EntryUri} \"{entry.Title}\")");
      else
        Console.WriteLine($"エントリの投稿に失敗しました");

      Console.ResetColor();
    }

    if (listFixedEntries && modifiedEntries != null && 0 < modifiedEntries.Count) {
      Console.WriteLine();
      Console.WriteLine("下記エントリに対して修正を行い再投稿しました。");

      foreach (var modifiedEntry in modifiedEntries) {
        Console.WriteLine("{0} \"{1}\"", modifiedEntry.EntryUri, modifiedEntry.Title);
      }
    }

    if (success) {
      Console.ForegroundColor = ConsoleColor.Green;
      Console.WriteLine("完了");
      Console.ResetColor();
    }
    else {
      Console.ForegroundColor = ConsoleColor.Red;
      Console.WriteLine("エラーにより中断しました");
      Console.ResetColor();
    }
  }

  private static void EditContent(IHatenaBlogEntryEditor editor, string input, string output)
  {
    static bool IsStdIO(string io)
      => string.IsNullOrEmpty(io) || string.Equals(io, "-", StringComparison.Ordinal);

    var encoding = new UTF8Encoding(false);

    using var inputStream = IsStdIO(input) ? Console.OpenStandardInput() : File.OpenRead(input);
    var reader = new StreamReader(inputStream, false);
    var modified = editor.Edit(
      new Entry { Content = reader.ReadToEnd() },
      out var originalText,
      out var modifiedText
    );

    using var outputStream = IsStdIO(output) ? Console.OpenStandardOutput() : File.Create(output);
    var writer = new StreamWriter(outputStream, encoding);

    if (modified)
      writer.Write(modifiedText);
    else
      writer.Write(originalText);

    writer.Flush();
  }

  private class EntryEditor : IHatenaBlogEntryEditor {
    private static readonly Predicate<Html.HtmlAttribute> defaultPredicateForFixMixedContent = (_) => true;

    private readonly bool fixMixedContent;
    private readonly Predicate<Html.HtmlAttribute> predicateForFixMixedContent;
    private readonly bool replaceBlogUrl;
    private readonly string[] blogDomains;

    public EntryEditor(
      string blogDomain,
      string customBlogDomain,
      bool fixMixedContent,
      Predicate<Html.HtmlAttribute> predicateForFixMixedContent,
      bool replaceBlogUrl
    )
    {
      if (replaceBlogUrl && blogDomain == null && customBlogDomain == null)
        throw new ArgumentException($"{nameof(blogDomain)} or {nameof(customBlogDomain)} must be specified");

      blogDomains = new[] { blogDomain, customBlogDomain }.Where(d => d != null).ToArray();

      this.fixMixedContent = fixMixedContent;
      this.predicateForFixMixedContent = predicateForFixMixedContent;
      this.replaceBlogUrl = replaceBlogUrl;
    }

    public bool Edit(Entry entry, out string originalText, out string modifiedText)
    {
      originalText = entry.Content;
      modifiedText = null;

      var contentEditor = new HatenaBlogContentEditor(originalText);
      var modified = false;

      if (fixMixedContent)
        modified |= contentEditor.FixMixedContentReferences(predicateForFixMixedContent ?? defaultPredicateForFixMixedContent);

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
