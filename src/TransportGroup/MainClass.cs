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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using Smdn.Xml;

namespace Smdn.HatenaBlogTools {
  class MainClass {
    private class HatenaGroupEntry : Entry {
      public string OriginalDate;
      public string OriginalHeading;

      public HatenaGroupEntry()
      {
        IsDraft = false;
      }
    }

    // TODO:
    // 1日あたりの投稿記事数は100件まで http://staff.hatenablog.com/entry/2012/01/24/162244
    // 開いたままのHTML要素を含む記事を投稿すると表示が崩れる
    public static void Main(string[] args)
    {
      string hatenaId = null;
      string blogId = null;
      string apiKey = null;
      string exportedGroupDocumentFile = null;

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

          case "-export":
            exportedGroupDocumentFile = args[++i];
            break;

          case "/help":
          case "-h":
          case "--help":
            Usage(null);
            break;

          default:
            break;
        }
      }

      if (string.IsNullOrEmpty(hatenaId))
        Usage("hatena-idを指定してください");

      if (string.IsNullOrEmpty(blogId))
        Usage("blog-idを指定してください");

      if (string.IsNullOrEmpty(apiKey))
        Usage("api-keyを指定してください");

      if (string.IsNullOrEmpty(exportedGroupDocumentFile))
        Usage("exported fileを指定してください");
      else if (!File.Exists(exportedGroupDocumentFile))
        Usage("exported fileが見つかりません");

      // エクスポートファイルの読み込み
      var exportedGroupDocument = new XmlDocument();

      exportedGroupDocument.Load(exportedGroupDocumentFile);

      // 新しいエクスポートファイル用のXmlDocument
      var transportedGroupDocument = new XmlDocument();

      transportedGroupDocument.AppendChild(transportedGroupDocument.CreateXmlDeclaration("1.0", "utf-8", null));

      var newExportGroupDiaryElement = transportedGroupDocument.AppendElement("diary");

      // エクスポートファイルからエントリを読み込み・分割したのち、はてなブログへ投稿する
      var atom = new Atom();
      var collectionUri = new Uri(string.Concat("http://blog.hatena.ne.jp/", hatenaId, "/", blogId, "/atom/entry"));

      atom.Credential = new NetworkCredential(hatenaId, apiKey);

      foreach (var entry in GetEntries(exportedGroupDocument)) {
        /*
         * 投稿
         */
        string transportedEntryUrl;

        for (;;) {
          HatenaBlog.WaitForCinnamon();

          Console.Write("エントリを投稿中 {0} \"{1}\" ... ", entry.OriginalDate, entry.Title);

          HttpStatusCode statusCode;

          var responseDocument = HatenaBlog.PostEntry(atom, collectionUri, entry, out statusCode);

          if (statusCode == HttpStatusCode.Created) {
            var nsmgrResponseDocument = new XmlNamespaceManager(responseDocument.NameTable);

            nsmgrResponseDocument.AddNamespace("atom", Namespaces.Atom);

            transportedEntryUrl = responseDocument.GetSingleNodeValueOf("atom:entry/atom:link[@rel='alternate']/@href", nsmgrResponseDocument);

            break; // 成功
          }
          else {
            Console.WriteLine("失敗しました ({0})", statusCode);

            if (ConsoleUtils.AskYesNo("中断しますか?")) {
              Console.WriteLine("エクスポートを中断しました。");
              return; // 中断
            }
            else {
              continue; // リトライ
            }
          }
        }

        /*
         * 新しいエクスポート文書のエントリを生成
         */
        var dayElement = (XmlElement)newExportGroupDiaryElement.SelectSingleNode(string.Format("day[@date='{0}']", entry.OriginalDate));

        if (dayElement == null) {
          dayElement = newExportGroupDiaryElement.AppendElement("day");
          dayElement.SetAttribute("date", entry.OriginalDate);
          dayElement.SetAttribute("title", entry.Title);
          dayElement.AppendText("\n");
        }

        dayElement.AppendText(string.Format("{0}\nこのエントリは [{1}] に移転しました。\n\n", entry.OriginalHeading, transportedEntryUrl));

        Console.WriteLine("完了 ({0})", transportedEntryUrl);
      }

      transportedGroupDocument.Save(Path.ChangeExtension(exportedGroupDocumentFile, ".transport.xml"));

      Console.WriteLine("全エントリの投稿が完了しました。");
    }

    private static void Usage(string format, params string[] args)
    {
      if (format != null) {
        Console.Error.Write("error: ");
        Console.Error.WriteLine(format, args);
      }

      Console.Error.WriteLine("usage:");
      Console.Error.WriteLine("  {0} -id <hatena-id> -blogid <blog-id> -apikey <api-key> -export <exported file>",
        System.IO.Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location));

      Environment.Exit(-1);
    }

    private static readonly Regex entryHeaderRegex = new Regex(@"^\*((?<timestamp>[0-9]+)|(?<name>[^\*]+))\*(\[(?<category>[^\[]+)\]\s*)*(?<title>.*)$", RegexOptions.Singleline);

    private static IEnumerable<HatenaGroupEntry> GetEntries(XmlDocument exportedGroupDocument)
    {
      foreach (XmlElement dayElement in exportedGroupDocument.SelectNodes("/diary/day")) {
        var entry = new HatenaGroupEntry();
        var dateString = dayElement.GetAttribute("date");
        var date = DateTime.ParseExact(dateString, "yyyy-MM-dd", null, DateTimeStyles.AssumeLocal);

        entry.OriginalDate = dateString;
        entry.OriginalHeading = null;
        entry.Updated = date;
        entry.Title = dayElement.GetAttribute("title");

        var reader = new StringReader(dayElement.GetSingleNodeValueOf("body/text()"));
        var content = new StringBuilder();
        var isDayContentEmpty = true;

        for (;;) {
          var line = reader.ReadLine();

          if (line == null)
            break;

          var match = entryHeaderRegex.Match(line);

          if (match.Success) {
            if (!isDayContentEmpty) {
              // yield current entry
              entry.Content = content.ToString();

              yield return entry;
            }

            // new entry
            entry = new HatenaGroupEntry();
            entry.OriginalDate = dateString;
            entry.OriginalHeading = line;

            var timestamp = match.Groups["timestamp"].Value;

            if (string.IsNullOrEmpty(timestamp))
              entry.Updated = date;
            else
              entry.Updated = UnixTimeStamp.ToLocalDateTime(int.Parse(timestamp));

            entry.Title = match.Groups["title"].Value;

            foreach (Capture c in match.Groups["category"].Captures) {
              entry.Categories.Add(c.Value);
            }

            content.Clear();
            isDayContentEmpty = false;
          }
          else {
            content.AppendLine(line);

            if (isDayContentEmpty && 0 < line.Trim().Length)
              isDayContentEmpty = false;
          }
        } // for each line

        // yield current entry
        entry.Content = content.ToString();

        yield return entry;
      } // for each day element
    }

    //private static readonly Regex idCallRegex = new Regex(@"((d|b|a|f|h|i|r|graph):|g:[a-zA-Z0-9_\-]{3,}:)id:(?<id>[a-zA-Z0-9_\-]{3,})", RegexOptions.Singleline);
  }
}
