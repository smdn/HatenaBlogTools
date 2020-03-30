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
      MovableType,
      HatenaDiary,

      Default = Atom,
    }

    private enum CategoryFilterMode {
      None,
      Exclude,
      Include,
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
      yield return "--format [hatena|mt|atom|atom-post]  : 出力形式を指定します";
      yield return "                                       hatena     : はてなダイアリー日記データ形式";
      yield return "                                       mt         : Movable Type形式";
      yield return "                                       atom       : Atomフィード形式(取得できる全内容)";
      yield return "                                       atom-post  : Atomフィード形式(投稿データのみ)";
      yield return "　                                     省略した場合は'atom'と同じ形式になります";
      yield return "--exclude-category <カテゴリ>  : 指定された<カテゴリ>を除外してダンプします(複数指定可)";
      yield return "--include-category <カテゴリ>  : 指定された<カテゴリ>のみを抽出してダンプします(複数指定可)";
      yield return "[出力ファイル名|-]             : ダンプした内容を保存するファイル名を指定します";
      yield return "                                 省略した場合、- を指定した場合は標準出力に書き込みます";
    }

    public void Run(string[] args)
    {
      if (!ParseCommonCommandLineArgs(ref args, out var credential))
        return;

      bool retrieveComments = false;
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

      var filterMode = CategoryFilterMode.None;
      HashSet<string> categoriesToFilter = null;

      if (0 < categoriesToExclude.Count && 0 < categoriesToInclude.Count) {
        Usage("--exclude-categoryと--include-categoryを同時に指定することはできません");
        return;
      }
      else {
        if (0 < categoriesToExclude.Count) {
          filterMode = CategoryFilterMode.Exclude;
          categoriesToFilter = categoriesToExclude;
        }
        else if (0 < categoriesToInclude.Count) {
          filterMode = CategoryFilterMode.Include;
          categoriesToFilter = categoriesToInclude;
        }
      }

      if (!Login(credential, out var hatenaBlog))
        return;

      var outputDocument = DumpEntries(hatenaBlog, (outputFormat == OutputFormat.AtomPostData), filterMode, categoriesToFilter, out var entries);

      if (outputDocument == null)
        return;

      // 結果を保存
      Console.Error.WriteLine("結果を保存しています");

      using (var outputStream = outputFile == "-"
             ? Console.OpenStandardOutput()
             : new FileStream(outputFile, FileMode.Create, FileAccess.Write)) {
        switch (outputFormat) {
          case OutputFormat.HatenaDiary:
            SaveAsHatenaDiary(entries, outputStream, retrieveComments);
            break;

          case OutputFormat.MovableType:
            SaveAsMovableType(entries, outputStream, hatenaBlog.BlogId, retrieveComments);
            break;

          case OutputFormat.Atom:
          case OutputFormat.AtomPostData:
            outputDocument.Save(outputStream);
            break;

          default:
            throw new NotSupportedException($"unsupported format: {outputFormat}");
        }
      }

      Console.Error.WriteLine("完了");
    }

    private static XDocument DumpEntries(HatenaBlogAtomPubClient hatenaBlog,
                                         bool removeNonPostData,
                                         CategoryFilterMode filterMode,
                                         HashSet<string> categoriesToFilter,
                                         out List<PostedEntry> entries)
    {
      var filteredEntries = new List<PostedEntry>();

      XDocument outputDocument = null;

      Console.Error.WriteLine("エントリをダンプ中 ...");

      hatenaBlog.EnumerateEntries((postedEntry, entryElement) => {
        if (outputDocument == null) {
          // オリジナルのレスポンス文書からlink[@rel=first/next], entry以外の要素をコピーしてヘッダ部を構築する
          outputDocument = new XDocument(entryElement.Document);

          outputDocument.Root
                        .Elements(AtomPub.Namespaces.Atom + "entry")
                        .Remove();

          outputDocument.Root
                        .Elements(AtomPub.Namespaces.Atom + "link")
                        .Where(e => e.HasAttributeWithValue("rel", "first") || e.HasAttributeWithValue("rel", "next"))
                        .Remove();
        }

        Console.Error.Write("{0} \"{1}\" : ",
                            postedEntry.EntryUri,
                            postedEntry.Title);

        switch (filterMode) {
          case CategoryFilterMode.Include:
            if (!postedEntry.Categories.Overlaps(categoriesToFilter)) {
              Console.Error.WriteLine("対象カテゴリを含まないため除外します ({0})", string.Join(", ", postedEntry.Categories));
              return; // continue
            }
            break;

          case CategoryFilterMode.Exclude:
            if (postedEntry.Categories.Overlaps(categoriesToFilter)) {
              Console.Error.WriteLine("対象カテゴリを含むため除外します: ({0})", string.Join(", ", postedEntry.Categories));
              return; // continue
            }
            break;

          default:
            break; // do nothing
        }

        var modifiedEntryElement = new XElement(entryElement);

        if (removeNonPostData) {
          modifiedEntryElement.Elements(AtomPub.Namespaces.Atom + "id").Remove();
          modifiedEntryElement.Elements(AtomPub.Namespaces.Atom + "link").Remove();
          modifiedEntryElement.Elements(AtomPub.Namespaces.App + "edited").Remove();
          modifiedEntryElement.Elements(AtomPub.Namespaces.Hatena + "formatted-content").Remove();
        }

        outputDocument.Root.Add(modifiedEntryElement);

        filteredEntries.Add(postedEntry);

        Console.Error.WriteLine("ダンプしました");
      });

      entries = filteredEntries;

      return outputDocument;
    }

    private static void SaveAsMovableType(IEnumerable<PostedEntry> entries, Stream outputStream, string blogId, bool retrieveComments)
    {
      /*
       * http://www.movabletype.jp/documentation/appendices/import-export-format.html
       */
      string ToMovableTypeDateString(DateTime dateTime)
      {
        return dateTime.ToString("MM/dd/yyyy hh\\:mm\\:ss tt", System.Globalization.CultureInfo.InvariantCulture);
      }

      var writer = new StreamWriter(outputStream, Encoding.UTF8);

      writer.NewLine = "\n"; // ???

      foreach (var entry in entries) {
        /*
         * metadata seciton
         */
        writer.WriteLine(string.Concat("AUTHOR: ", entry.Author));
        writer.WriteLine(string.Concat("TITLE: ", entry.Title));

        var entryLocation = entry.EntryUri?.LocalPath;

        if (entryLocation != null)
          writer.WriteLine(string.Concat("BASENAME: ", entryLocation.Substring(7))); // remove prefix '/entry/'

        writer.WriteLine(string.Concat("STATUS: ", entry.IsDraft ? "Draft" : "Publish"));
        writer.WriteLine("CONVERT BREAKS: 0");

        if (entry.Updated.HasValue)
          writer.WriteLine(string.Concat("DATE: ", ToMovableTypeDateString(entry.Updated.Value.LocalDateTime)));

        var tags = entry.Categories
                        .Select(tag => tag.Contains(" ") ? string.Concat("\"", tag, "\"") : tag);

        writer.WriteLine(string.Concat("TAGS: ", string.Join(",", tags)));

        /*
         * multiline field seciton
         */
        const string multilineFieldDelimiter = "-----";

        writer.WriteLine(multilineFieldDelimiter);

        writer.WriteLine("BODY:");
        //writer.WriteLine(entry.Content);
        writer.WriteLine(entry.FormattedContent);
        writer.WriteLine(multilineFieldDelimiter);

#if RETRIEVE_COMMENTS
        if (retrieveComments) {
          var entryUrl = entry.GetSingleNodeValueOf("atom:link[@rel='alternate' and @type='text/html']/@href", nsmgr);

          foreach (var comment in RetrieveComments(entryUrl)) {
            writer.WriteLine("COMMENT:");
            writer.WriteLine(string.Concat("AUTHOR: ", comment.Author));
            writer.WriteLine(string.Concat("DATE: ", ToMovableTypeDateString(comment.Date)));
            writer.WriteLine(string.Concat("URL: ", comment.Url));
            writer.WriteLine(comment.Content);

            writer.WriteLine(multilineFieldDelimiter);
          }
        }
#endif

        // end of entry
        const string entryDelimiter = "--------";

        writer.WriteLine(entryDelimiter);
      }

      writer.Flush();
    }

    private static void SaveAsHatenaDiary(IEnumerable<PostedEntry> entries, Stream outputStream, bool retrieveComments)
    {
      var diaryElement = new XElement("diary");
      var dayElements = new Dictionary<string, XElement>();
      var defaultUpdatedDate = DateTimeOffset.FromUnixTimeSeconds(0L);

      foreach (var entry in entries) {
        var updatedDate = entry.Updated ?? defaultUpdatedDate;
        var date = updatedDate.ToLocalTime()
                              .DateTime
                              .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        XElement dayElement, bodyElement;

        if (dayElements.TryGetValue(date, out dayElement)) {
          bodyElement = dayElement.Element("body");
        }
        else {
          bodyElement = new XElement("body");

          dayElement = new XElement(
            "day",
            new XAttribute("date", date),
            new XAttribute("title", string.Empty),
            bodyElement
          );

          diaryElement.Add(dayElement);

          dayElements[date] = dayElement;
        }

        var body = new StringBuilder();

        body.AppendFormat("*{0}*", updatedDate.ToUnixTimeSeconds());

        var joinedCategory = string.Join("][", entry.Categories);

        if (0 < joinedCategory.Length)
          body.AppendFormat("[{0}]", joinedCategory);

        body.AppendLine(entry.Title);

        body.AppendLine(entry.Content);
        body.AppendLine();

        bodyElement.Add(new XText(body.ToString()));

#if RETRIEVE_COMMENTS
        if (retrieveComments) {
          var entryUrl = entry.GetSingleNodeValueOf("atom:link[@rel='alternate' and @type='text/html']/@href", nsmgr);
          var commentsElement = dayElement.AppendElement("comments");

          foreach (var comment in RetrieveComments(entryUrl)) {
            var commentElement = commentsElement.AppendElement("comment");

            commentElement.AppendElement("username").AppendText(comment.Author);
            commentElement.AppendElement("body").AppendText(comment.Content);
            commentElement.AppendElement("timestamp").AppendText(XmlConvert.ToString(comment.Date.ToUnixTimeSeconds()));
          }
        }
#endif
      }

      var outputDocument = new XDocument(new XDeclaration("1.0", "utf-8", null),
                                         diaryElement);

      outputDocument.Save(outputStream);
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
