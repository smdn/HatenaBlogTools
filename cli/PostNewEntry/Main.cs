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
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;

using Smdn.Applications.HatenaBlogTools.AtomPublishingProtocol;
using Smdn.Applications.HatenaBlogTools.HatenaBlog;
using Smdn.Xml.Linq;

namespace Smdn.Applications.HatenaBlogTools {
  partial class PostNewEntry : CliBase {
    protected override string GetDescription() => "指定された内容で新しい記事を投稿します。";

    protected override string GetUsageExtraMandatoryOptions() => "--title <タイトル> --category <カテゴリ1> --category <カテゴリ2> <本文>";

    protected override IEnumerable<string> GetUsageExtraOptionDescriptions()
    {
      yield return "--title <タイトル>     : 投稿する記事のタイトルを指定します";
      yield return "--category <カテゴリ>  : 投稿する記事に設定するカテゴリを指定します";
      yield return "                         \"--category カテゴリ1 --category カテゴリ2\"のように指定することで";
      yield return "                         複数のカテゴリを指定することができます";
      yield return "--draft                : 記事を下書きとして投稿します";
      yield return "";
      yield return "<本文>                 : 投稿する記事の本文を指定します";
      yield return "--from-file <ファイル> : 指定された<ファイル>の内容を本文として投稿します";
      yield return "--from-file -          : 標準入力に与えられた内容を本文として投稿します";
    }

    public void Run(string[] args)
    {
      if (!ParseCommonCommandLineArgs(ref args, out HatenaBlogAtomPubCredential credential))
        return;

      var entry = new Entry();
      string contentFile = null;

      for (var i = 0; i < args.Length; i++) {
        switch (args[i]) {
          case "--title":
          case "-title":
            entry.Title = args[++i];
            break;

          case "--category":
          case "-category":
            entry.Categories.Add(args[++i]);
            break;

          case "--draft":
          case "-draft":
            entry.IsDraft = true;
            break;

          case "--from-file":
          case "-fromfile":
            contentFile = args[++i];
            break;

          default:
            entry.Content = args[i];
            break;
        }
      }

      if (!string.IsNullOrEmpty(contentFile)) {
        if (contentFile == "-") {
          using (var reader = new StreamReader(Console.OpenStandardInput())) {
            entry.Content = reader.ReadToEnd();
          }
        }
        else if (File.Exists(contentFile)) {
          entry.Content = File.ReadAllText(contentFile);
        }
        else {
          Usage("ファイル '{0}' が見つかりません", contentFile);
        }
      }

      if (!Login(credential, out HatenaBlogAtomPubClient hatenaBlog))
        return;

      Console.Write("投稿しています ... ");

      try {
        var statusCode = hatenaBlog.PostEntry(entry, out XDocument responseDocument);

        if (statusCode == HttpStatusCode.Created) {
          var createdUri = responseDocument.Element(AtomPub.Namespaces.Atom + "entry")
                                           ?.Elements(AtomPub.Namespaces.Atom + "link")
                                           ?.FirstOrDefault(e => e.HasAttributeWithValue("rel", "alternate"))
                                           ?.GetAttributeValue("href");

          Console.ForegroundColor = ConsoleColor.Green;
          Console.WriteLine("完了しました: {0}", createdUri);
          Console.ResetColor();
        }
        else {
          Console.ForegroundColor = ConsoleColor.Red;
          Console.Error.WriteLine("失敗しました: {0}", statusCode);
          Console.ResetColor();
        }
      }
      catch (PostEntryFailedException ex) {
        Console.WriteLine();
        Console.Error.WriteLine(ex);

        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine("失敗しました");
        Console.ResetColor();
      }
    }
  }
}
