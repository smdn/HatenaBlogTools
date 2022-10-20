// SPDX-FileCopyrightText: 2014 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;

using Smdn.HatenaBlogTools.HatenaBlog;

namespace Smdn.HatenaBlogTools;

public partial class ReplaceContentText : CliBase {
  protected override string GetDescription() => "すべての記事に対して置換を行います。";

  protected override string GetUsageExtraMandatoryOptions() => "--from <置換前の文字列> [--to <置換後の文字列>]";

  protected override IEnumerable<string> GetUsageExtraOptionDescriptions()
  {
    yield return "--from <置換前の文字列>    : 置換したい文字列を指定します";
    yield return "--to <置換後の文字列>      : 置換後の文字列を指定します";
    yield return "--regex                    : --fromおよび--toで指定された文字列を正規表現として解釈します";
    yield return "--replace-title-only       : 記事のタイトルのみを置換します";
    yield return "                             (デフォルトでは本文のみを置換します)";
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
                                    new[] { "-diff-test" },
                                    out var credential)) {
      return;
    }

    string replaceFromText = null;
    string replaceToText = null;
    bool replaceAsRegex = false;
    bool replaceTitleInsteadOfContent = false;
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

        case "--replace-title-only":
          replaceTitleInsteadOfContent = true;
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

    var descriptionOfReplacementTarget = replaceTitleInsteadOfContent
      ? "タイトル"
      : "本文";

    var diffGenerator = DiffGenerator.Create(false,
                                             diffCommand,
                                             diffCommandArgs,
                                             $"置換前の{descriptionOfReplacementTarget}",
                                             $"置換前の{descriptionOfReplacementTarget}");

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

    var modifier = replaceTitleInsteadOfContent
      ? (EntryTextModifier)new EntryTitleModifier()
      : (EntryTextModifier)new EntryContentModifier();

    var editor = replaceAsRegex
      ? (IHatenaBlogEntryEditor)new RegexEntryEditor(replaceFromText, replaceToText, modifier)
      : (IHatenaBlogEntryEditor)new EntryEditor(replaceFromText, replaceToText, modifier);

    var postMode = dryrun
      ? HatenaBlogFunctions.PostMode.PostNever
      : HatenaBlogFunctions.PostMode.PostIfModified;

    Func<bool> confirmBeforePosting = null;

    if (confirm)
      confirmBeforePosting = () => ConsoleUtils.AskYesNo(false, "更新しますか");

    if (!Login(credential, out var hatenaBlog))
      return;

    var success = true;

    try {
      HatenaBlogFunctions.EditAllEntry(
        hatenaBlog,
        postMode,
        editor,
        diffGenerator,
        entryUrlSkipTo: null,
        confirmBeforePosting,
        out _,
        out _
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
}
