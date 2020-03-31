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

#undef RETRIEVE_COMMENTS

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using Smdn.Applications.HatenaBlogTools.AtomPublishingProtocol;
using Smdn.Applications.HatenaBlogTools.HatenaBlog;
using Smdn.Xml.Linq;

namespace Smdn.Applications.HatenaBlogTools {
  partial class DumpAllEntries : CliBase {
    private enum OutputFormat {
      Atom,
      AtomPostData,
      AtomBlogger,
      MovableType,
      HatenaDiary,

      Default = Atom,
    }

#if RETRIEVE_COMMENTS
    private class Comment {
      public string Content;
      public string Author;
      public string Url;
      public DateTime Date;
    }
#endif

    protected override string GetDescription() => "すべてのブロク記事をダンプします。";

    protected override string GetUsageExtraMandatoryOptions() => "[--format [hatena|mt|atom]] [出力ファイル名|-]";

    protected override IEnumerable<string> GetUsageExtraOptionDescriptions()
    {
#if RETRIEVE_COMMENTS
      yield return "-comment : dump comments posted on entry"
#endif
      yield return "--format [hatena|mt|atom|atom-post|atom-blogger] : 出力形式を指定します";
      yield return "  --format hatena       : はてなダイアリー日記データ形式";
      yield return "  --format mt           : Movable Type形式";
      yield return "  --format atom         : Atomフィード形式(はてなブログAPIで取得できる全内容)";
      yield return "  --format atom-post    : Atomフィード形式(はてなブログAPIで取得できる内容のうち、投稿データのみ抽出)";
      yield return "  --format atom-blogger : Atomフィード形式(Blogger用フォーマット)";
      yield return "　(省略した場合は、'--format atom'を指定した場合と同じ形式で出力します)";
      yield return "";
      yield return "--exclude-category <カテゴリ>  : 指定された<カテゴリ>を除外してダンプします(複数指定可)";
      yield return "--include-category <カテゴリ>  : 指定された<カテゴリ>のみを抽出してダンプします(複数指定可)";
      yield return "";
      yield return "[出力ファイル名|-]             : ダンプした内容を保存するファイル名を指定します";
      yield return "                                 省略した場合、- を指定した場合は標準出力に書き込みます";
    }

    public void Run(string[] args)
    {
      if (!ParseCommonCommandLineArgs(ref args, out var credential))
        return;

#if RETRIEVE_COMMENTS
      bool retrieveComments = false;
#endif
      var categoriesToExclude = new HashSet<string>(StringComparer.Ordinal);
      var categoriesToInclude = new HashSet<string>(StringComparer.Ordinal);
      OutputFormat outputFormat = OutputFormat.Default;
      string outputFile = "-";

      for (var i = 0; i < args.Length; i++) {
        switch (args[i]) {
          case "--format":
          case "-format":
            var format = args[++i];

            switch (format) {
              case "hatena":
                outputFormat = OutputFormat.HatenaDiary;
                break;

              case "mt":
                outputFormat = OutputFormat.MovableType;
                break;

              case "atom":
                outputFormat = OutputFormat.Atom;
                break;

              case "atom-post":
                outputFormat = OutputFormat.AtomPostData;
                break;

              case "atom-blogger":
                outputFormat = OutputFormat.AtomBlogger;
                break;

              default:
                Usage("unsupported format: {0}", format);
                break;
            }
            break;

          case "--exclude-category":
          case "-excat":
            categoriesToExclude.Add(args[++i]);
            break;

          case "--include-category":
          case "-incat":
            categoriesToInclude.Add(args[++i]);
            break;

#if RETRIEVE_COMMENTS
          case "-comment":
            retrieveComments = true;
            break;
#endif

          default:
            outputFile = args[i];
            break;
        }
      }

      if (0 < categoriesToExclude.Count && 0 < categoriesToInclude.Count) {
        Usage("--exclude-categoryと--include-categoryを同時に指定することはできません");
        return;
      }

      if (!Login(credential, out var hatenaBlog))
        return;

      Predicate<PostedEntry> entryPredicate;

      if (0 < categoriesToExclude.Count) {
        entryPredicate = entry => {
          Console.Error.Write($"{entry.EntryUri} \"{entry.Title}\" : ");

          if (entry.Categories.Overlaps(categoriesToExclude)) {
            Console.Error.WriteLine("対象カテゴリを含むため除外します: ({0})", string.Join(", ", entry.Categories));
            return false;
          }
          else {
            Console.Error.WriteLine("完了");
            return true;
          }
        };
      }
      else if (0 < categoriesToInclude.Count)
        entryPredicate = entry => {
          Console.Error.Write($"{entry.EntryUri} \"{entry.Title}\" : ");

          if (entry.Categories.Overlaps(categoriesToInclude)) {
            Console.Error.WriteLine("完了");
            return true;
          }
          else {
            Console.Error.WriteLine("対象カテゴリを含まないため除外します ({0})", string.Join(", ", entry.Categories));
            return false;
          }
        };
      else {
        entryPredicate = entry => {
          Console.Error.Write($"{entry.EntryUri} \"{entry.Title}\" : ");
          Console.Error.WriteLine("完了");
          return true;
        };
      }

      var outputDocument = DumpEntries(hatenaBlog, entryPredicate, out var entries);

      if (outputDocument == null)
        return;

      // 結果を保存
      Console.Error.WriteLine("結果を保存しています");

      using (var outputStream = outputFile == "-"
             ? Console.OpenStandardOutput()
             : new FileStream(outputFile, FileMode.Create, FileAccess.Write)) {
        switch (outputFormat) {
          case OutputFormat.HatenaDiary:
            new HatenaDiaryFormatter(/*retrieveComments*/).Format(entries, outputStream);
            break;

          case OutputFormat.MovableType:
            new MovableTypeFormatter(/*retrieveComments*/).Format(entries, outputStream);
            break;

          case OutputFormat.AtomBlogger:
            new BloggerFormatter(hatenaBlog.BlogTitle /*, retrieveComments*/).Format(entries, outputStream);
            break;

          case OutputFormat.AtomPostData:
            var elementsEntry = outputDocument.Root.Elements(AtomPub.Namespaces.Atom + "entry");

            elementsEntry.Elements(AtomPub.Namespaces.Atom + "id").Remove();
            elementsEntry.Elements(AtomPub.Namespaces.Atom + "link").Remove();
            elementsEntry.Elements(AtomPub.Namespaces.App + "edited").Remove();
            elementsEntry.Elements(AtomPub.Namespaces.Hatena + "formatted-content").Remove();

            goto case OutputFormat.Atom;

          case OutputFormat.Atom:
            outputDocument.Save(outputStream);
            break;

          default:
            throw new NotSupportedException($"unsupported format: {outputFormat}");
        }
      }

      Console.Error.WriteLine("完了");
    }

    private static XDocument DumpEntries(
      HatenaBlogAtomPubClient hatenaBlog,
      Predicate<PostedEntry> entryPredicate,
      out IReadOnlyList<PostedEntry> entries
    )
    {
      var filteredEntries = new List<PostedEntry>();

      XDocument outputDocument = null;

      Console.Error.WriteLine("エントリをダンプ中 ...");

      hatenaBlog.EnumerateEntries((postedEntry, entryElement) => {
        if (outputDocument == null) {
          // オリジナルのレスポンス文書からlink[@rel=first/next], entry以外の要素をコピーしてヘッダ部を構築する
          outputDocument = new XDocument(entryElement.Document);

          outputDocument
            .Root
            .Elements(AtomPub.Namespaces.Atom + "entry")
            .Remove();

          outputDocument
            .Root
            .Elements(AtomPub.Namespaces.Atom + "link")
            .Where(e => e.HasAttributeWithValue("rel", "first") || e.HasAttributeWithValue("rel", "next"))
            .Remove();
        }

        if (!entryPredicate(postedEntry))
          return; // continue

        outputDocument.Root.Add(new XElement(entryElement));

        filteredEntries.Add(postedEntry);
      });

      entries = filteredEntries;

      return outputDocument;
    }

#if RETRIEVE_COMMENTS
    /// <remarks>コメントはJavaScriptによって動的に読み込まれているので、この方法では取得できない</remarks>
    private static IEnumerable<Comment> RetrieveComments(string entryUrl)
    {
      var doc = new XmlDocument();

      Console.Error.Write("{0} のコメントを取得中 ... ", entryUrl);

      using (var sgmlReader = new Sgml.SgmlReader()) {
        sgmlReader.Href = entryUrl;
        sgmlReader.CaseFolding = Sgml.CaseFolding.ToLower;

        doc.Load(sgmlReader);

        System.Threading.Thread.Sleep(500);
      }

      //var contentNode = doc.GetElementById("content");
      var contentNode = doc.SelectSingleNode("//*[@id = 'content']");

      foreach (XmlElement commentRootElement in contentNode.SelectNodes(".//*[contains(@class, 'entry-comment')]")) {
        var comment = new Comment();

        foreach (XmlNode commentChildNode in commentRootElement.ChildNodes) {
          if (commentChildNode.NodeType != XmlNodeType.Element)
            continue;

          var commentChildElement = (XmlElement)commentChildNode;

          switch (commentChildElement.GetAttribute("class")) {
            case "comment-user-name":
              /* 
               * <!-- hatena user -->
               * <e class="comment-user-name">
               *   <a class="comment-user-id" href="http://blog.hatena.ne.jp/hatenaid/">
               *     <span class="comment-nickname" data-user-name="hatenaid">
               *       id:hatenaid
               *     </span>
               *   </a>
               * </e>
               * <!-- name with website -->
               * <e class="comment-user-name">
               *   name
               *   <a class="icon-website" href="http://example.com/" />
               * </e>
               * <!-- name only -->
               * <e class="comment-user-name">
               *   name
               * </e>
               */
              comment.Author = commentChildElement.InnerText.Trim();
              comment.Url = commentChildElement.GetSingleNodeValueOf(".//@href");
              break;

            case "comment-content":
              /* 
               * <e class="comment-content">
               *   <p>comment-html</p>
               * </e>
               */
              comment.Content = commentChildElement.FirstChild.InnerXml;
              break;

            case "comment-metadata":
              /* 
               * <e class="comment-metadata">
               *   <time data-epoch="1387283661000" />
               * </e>
               */
              comment.Date = DateTimeOffset.FromUnixTime(commentChildElement.GetSingleNodeValueOf("time/@data-epoch", long.Parse) / 1000).ToLocalTime();
              break;
          }
        }

        yield return comment;
      }

      Console.Error.WriteLine("完了");
    }
#endif
  }
}
