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
using System.Linq;
using System.Net;

using Smdn.Applications.HatenaBlogTools.HatenaBlog;

using CategorySet = System.Collections.Generic.HashSet<string>;

namespace Smdn.Applications.HatenaBlogTools {
  class CategoryModification {
    public CategorySet Old { get; }
    public CategorySet New { get; }

    public CategoryModification(CategorySet old, CategorySet @new)
    {
      this.Old = old ?? throw new ArgumentNullException(nameof(old));
      this.New = @new ?? throw new ArgumentNullException(nameof(old));
    }

    public void Apply(CategorySet categories)
    {
      if (Old.Count == 0) {
        if (categories.Count == 0)
          // カテゴリが設定されていない場合、新規設定
          categories.UnionWith(New);
      }
      else if (categories.IsSupersetOf(Old)) {
        // カテゴリがすべて設定されている場合、置換または削除
        categories.ExceptWith(Old);
        categories.UnionWith(New);
      }
    }
  }

  partial class ModifyCategory : CliBase {
    protected override string GetDescription() => "記事のカテゴリを一括変更します。";

    protected override string GetUsageExtraMandatoryOptions() => "\"旧カテゴリ1:新カテゴリ1\" \"旧カテゴリ2:新カテゴリ2\" ...";

    protected override IEnumerable<string> GetUsageExtraOptionDescriptions()
    {
      yield return "-n, --dry-run        : 変更内容の確認だけ行い、変更の投稿は行いません";
      yield return "";
      yield return "変更カテゴリの指定(例):";
      yield return "  <old>:<new>        : カテゴリ<old>を<new>に置き換えます";
      yield return "  <old>:             : カテゴリ<old>を削除します";
      yield return "  :<new>             : カテゴリが設定されていない記事にカテゴリ<new>を設定します";
      yield return "  <old>:<old>;<new>  : カテゴリ<old>の記事にカテゴリ<new>を追加します";
      yield return "";
      yield return "カテゴリ指定の構文:";
      yield return "  <置換前のカテゴリグループ>:<置換後のカテゴリグループ>";
      yield return "  ";
      yield return "  置換前後のカテゴリグループには;で区切ることで1つ以上のカテゴリを指定できます";
      yield return "  ";
      yield return "  \"a;b;c...:A;B;C...\"と指定した場合、記事にカテゴリa, b, c, ...の'すべて'が";
      yield return "  設定されている場合に、新たなカテゴリA, B, C, ...への置換を行います";
      yield return "  ";
      yield return "  置換前後で異なるカテゴリ数にすることもできます";
      yield return "  ";
      yield return "  \"a;b;c...:A\"と指定した場合、記事にカテゴリa, b, c, ...の'すべて'が";
      yield return "  設定されている場合に、カテゴリAへと統合します";
      yield return "  ";
      yield return "  置換後のカテゴリグループに何も指定しない場合、空のカテゴリへの置換となります";
      yield return "  つまり、置換前のカテゴリが'削除されます'";
      yield return "";
      yield return "  \"a;b:\"と指定した場合、記事にカテゴリaとbが設定されている場合に、カテゴリの設定を削除します";
      yield return "  ";
      yield return "  置換前のカテゴリグループに何も指定しない場合、空のカテゴリからの置換となります";
      yield return "  つまり、'カテゴリが設定されてない記事に対してのみ'置換を行います";
    }

    public void Run(string[] args)
    {
      if (!ParseCommonCommandLineArgs(ref args, out var credential))
        return;

      bool dryrun = false;
      var categoryModifications = new List<CategoryModification>();

      for (var i = 0; i < args.Length; i++) {
        switch (args[i]) {
          case "--dry-run":
          case "-n":
            dryrun = true;
            break;

          default: {
            var pos = args[i].IndexOf(':');

            if (0 <= pos) {
              var categoriesOld = args[i].Substring(0, pos).Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
              var categoriesNew = args[i].Substring(pos + 1).Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);

              if (0 < categoriesOld.Length || 0 < categoriesNew.Length)
                categoryModifications.Add(new CategoryModification(new CategorySet(categoriesOld, StringComparer.Ordinal),
                                                                   new CategorySet(categoriesNew, StringComparer.Ordinal)));
            }

            break;
          }
        }
      }

      if (categoryModifications.Count <= 0)
        Usage("変更するカテゴリを指定してください");

      Console.WriteLine("以下のカテゴリを変更します");

      foreach (var modification in categoryModifications) {
        if (modification.Old.Count == 0)
          Console.WriteLine("(新規設定) -> {0}", Join(modification.New));
        else if (modification.New.Count == 0)
          Console.WriteLine("{0} -> (削除)", Join(modification.Old));
        else
          Console.WriteLine("{0} -> {1}", Join(modification.Old), Join(modification.New));
      }

      Console.WriteLine();

      if (!Login(credential, out var hatenaBlog))
        return;

      Console.WriteLine("エントリを取得中 ...");

      List<PostedEntry> entries = null;

      try {
        entries = hatenaBlog.EnumerateEntries().ToList();
      }
      catch (WebException ex) {
        Console.Error.WriteLine(ex.Message);
        return;
      }

      Console.WriteLine("以下のエントリのカテゴリが変更されます");

      var modifiedEntries = new List<PostedEntry>(entries.Count);

      foreach (var entry in entries) {
        var prevCategories = new CategorySet(entry.Categories, entry.Categories.Comparer);

        foreach (var modification in categoryModifications) {
          modification.Apply(entry.Categories);
        }

        if (prevCategories.SetEquals(entry.Categories))
          continue; // 変更なし

        modifiedEntries.Add(entry);

        Console.WriteLine("{0} \"{1}\" {2} -> {3}",
                          entry.EntryUri,
                          entry.Title,
                          Join(prevCategories),
                          Join(entry.Categories));
      }

      if (modifiedEntries.Count == 0) {
        Console.WriteLine("カテゴリが変更されるエントリはありません");
        return;
      }

      if (dryrun)
        return;

      if (!ConsoleUtils.AskYesNo("変更しますか ")) {
        Console.WriteLine("変更を中断しました");
        return;
      }

      Console.WriteLine();

      foreach (var entry in modifiedEntries) {
        Console.Write("変更を更新中: {0} \"{1}\" [{2}] ... ",
                      entry.Published,
                      entry.Title,
                      string.Join("][", entry.Categories));

        var statusCode = hatenaBlog.UpdateEntry(entry, out _);

        if (statusCode == HttpStatusCode.OK) {
          hatenaBlog.WaitForCinnamon();

          Console.ForegroundColor = ConsoleColor.Green;
          Console.WriteLine("更新しました");
          Console.ResetColor();
        }
        else {
          Console.ForegroundColor = ConsoleColor.Red;
          Console.WriteLine("失敗しました ({0})", statusCode);
          Console.ResetColor();
          return;
        }
      }

      Console.WriteLine("変更が完了しました");
    }

    private static string Join(CategorySet categorySet)
    {
      if (categorySet.Count == 0)
        return "(カテゴリなし)";
      else
        return "[" + string.Join("][", categorySet) + "]";
    }
  }
}
