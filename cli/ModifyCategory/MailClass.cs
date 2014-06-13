//
// Author:
//       smdn <smdn@smdn.jp>
//
// Copyright (c) 2013-2014 smdn
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
using System.Xml;

using Smdn.Xml;

using CategorySet = System.Collections.Generic.HashSet<string>;

namespace Smdn.Applications.HatenaBlogTools {
  class CategoryModification {
    public CategorySet Old { get; private set; }
    public CategorySet New { get; private set; }

    public CategoryModification(CategorySet old, CategorySet @new)
    {
      this.Old = old;
      this.New = @new;
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

  class MainClass {
    public static void Main(string[] args)
    {
      string hatenaId = null;
      string blogId = null;
      string apiKey = null;
      bool dryrun = false;
      var categoryModifications = new List<CategoryModification>();

      for (var i = 0; i < args.Length; i++) {
        switch (args[i]) {
          case "-id":
            hatenaId = args[++i];
            break;

          case "-blogid":
            blogId = args[++i];
            break;

          case "-apikey":
            apiKey = args[++i];
            break;

          case "-n":
            dryrun = true;
            break;

          case "/help":
          case "-h":
          case "--help":
            Usage(null);
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

      if (string.IsNullOrEmpty(hatenaId))
        Usage("hatena-idを指定してください");

      if (string.IsNullOrEmpty(blogId))
        Usage("blog-idを指定してください");

      if (string.IsNullOrEmpty(apiKey))
        Usage("api-keyを指定してください");

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
      Console.WriteLine("エントリを取得中 ...");

      List<PostedEntry> entries = null;

      try {
        entries = HatenaBlog.GetEntries(hatenaId, blogId, apiKey);
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
                          entry.Published,
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

      var atom = new Atom();

      atom.Credential = new NetworkCredential(hatenaId, apiKey);

      foreach (var entry in modifiedEntries) {
        Console.Write("変更を更新中: {0} \"{1}\" [{2}] ... ",
                      entry.Published,
                      entry.Title,
                      string.Join("][", entry.Categories));

        HttpStatusCode status;

        HatenaBlog.UpdateEntry(atom, entry, out status);

        if (status == HttpStatusCode.OK) {
          HatenaBlog.WaitForCinnamon();
          Console.WriteLine();
        }
        else {
          Console.WriteLine("失敗しました ({0})", status);
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

    private static void Usage(string format, params string[] args)
    {
      if (format != null) {
        Console.Error.Write("error: ");
        Console.Error.WriteLine(format, args);
      }

      Console.Error.WriteLine("usage:");
      Console.Error.WriteLine("  {0} -id <hatena-id> -blogid <blog-id> -apikey <api-key> old1:new1 old2:new2 ...",
                              System.IO.Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location));
      Console.Error.WriteLine("options:");
      Console.Error.WriteLine("  -n : dry run");

      Environment.Exit(-1);
    }
  }
}
