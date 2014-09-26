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

#undef RETRIEVE_COMMENTS

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml;

using Smdn.Xml;

namespace Smdn.Applications.HatenaBlogTools {
  class MainClass {
    private enum OutputFormat {
      Default,
      MovableType,
      HatenaDiary,
    }

#if RETRIEVE_COMMENTS
    private class Comment {
      public string Content;
      public string Author;
      public string Url;
      public DateTime Date;
    }
#endif

    public static void Main(string[] args)
    {
      string hatenaId = null;
      string blogId = null;
      string apiKey = null;
      bool retrieveComments = false;
      var categoriesToExclude = new HashSet<string>(StringComparer.Ordinal);
      var categoriesToInclude = new HashSet<string>(StringComparer.Ordinal);
      OutputFormat outputFormat = OutputFormat.Default;
      string outputFile = "-";

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

          case "-format":
            var format = args[++i];

            switch (format) {
              case "hatena":
                outputFormat = OutputFormat.HatenaDiary;
                break;
              case "mt":
                outputFormat = OutputFormat.MovableType;
                break;
              default:
                Usage("unsupported format: {0}", format);
                break;
            }
            break;

          case "-excat":
            categoriesToExclude.Add(args[++i]);
            break;

          case "-incat":
            categoriesToInclude.Add(args[++i]);
            break;

#if RETRIEVE_COMMENTS
          case "-comment":
            retrieveComments = true;
            break;
#endif

          case "/help":
          case "-h":
          case "--help":
            Usage(null);
            break;

          default:
            outputFile = args[i];
            break;
        }
      }

      if (string.IsNullOrEmpty(hatenaId))
        Usage("hatena-idを指定してください");

      if (string.IsNullOrEmpty(blogId))
        Usage("blog-idを指定してください");

      if (string.IsNullOrEmpty(apiKey))
        Usage("api-keyを指定してください");

      if (0 < categoriesToExclude.Count && 0 < categoriesToInclude.Count)
        Usage("-excatと-incatを同時に指定することはできません");

#if false
      var outputDocument = DumpAllEntries(hatenaId, blogId, apiKey);

      if (outputDocument == null)
        return;
#endif
      var outputDocument = new XmlDocument();
      outputDocument.Load("/home/smdn/dummy.xml");

      if (0 < categoriesToExclude.Count) {
        Console.Error.WriteLine("次のカテゴリの記事を除外しています: {0}", string.Join(", ", categoriesToExclude));

        FilterEntries(outputDocument, categoriesToExclude, true); // exclude specified categories
      }
      else if (0 < categoriesToInclude.Count) {
        Console.Error.WriteLine("次のカテゴリの記事を抽出しています: {0}", string.Join(", ", categoriesToInclude));

        FilterEntries(outputDocument, categoriesToInclude, false); // include specified categories
      }

      // 結果を保存
      Console.Error.WriteLine("結果を保存しています");

      using (var outputStream = outputFile == "-"
             ? Console.OpenStandardOutput()
             : new FileStream(outputFile, FileMode.Create, FileAccess.Write)) {

        switch (outputFormat) {
          case OutputFormat.HatenaDiary:
            SaveAsHatenaDiary(outputDocument, outputStream, retrieveComments);
            break;

          case OutputFormat.MovableType:
            SaveAsMovableType(outputDocument, outputStream, blogId, retrieveComments);
            break;

          case OutputFormat.Default:
          default:
            outputDocument.Save(outputStream);
            break;
        }
      }

      Console.Error.WriteLine("完了");
    }

    private static XmlDocument DumpAllEntries(string hatenaId, string blogId, string apiKey)
    {
      var atom = new Atom();

      atom.Credential = new NetworkCredential(hatenaId, apiKey);

      var rootEndPoint = new Uri(string.Concat("http://blog.hatena.ne.jp/", hatenaId, "/", blogId, "/atom/"));
      var nextUri = new Uri(rootEndPoint, "./entry");
      HttpStatusCode statusCode;
      XmlDocument outputDocument = null;

      Console.Error.Write("エントリをダンプ中 ");

      for (;;) {
        var collectionDocument = atom.Get(nextUri, out statusCode);

        if (statusCode == HttpStatusCode.OK) {
          Console.Error.Write(".");
        }
        else {
          Console.Error.WriteLine("エントリの取得に失敗したため中断しました ({0})", statusCode);
          return null;
        }

        var nsmgr = new XmlNamespaceManager(collectionDocument.NameTable);

        nsmgr.AddNamespace("atom", Namespaces.Atom);

        // 次のatom:linkを取得する
        nextUri = collectionDocument.GetSingleNodeValueOf("/atom:feed/atom:link[@rel='next']/@href", nsmgr, s => s == null ? null : new Uri(s));

        if (outputDocument == null) {
          // link[@rel=first/next]を削除してルート要素以下をコピーする
          collectionDocument.SelectSingleNode("/atom:feed/atom:link[@rel='first']", nsmgr).RemoveSelf();
          collectionDocument.SelectSingleNode("/atom:feed/atom:link[@rel='next']", nsmgr).RemoveSelf();

          outputDocument = new XmlDocument();

          outputDocument.AppendChild(outputDocument.ImportNode(collectionDocument.DocumentElement, true));
        }
        else {
          // atom:entryのみをコピーする
          foreach (XmlNode entry in collectionDocument.SelectNodes("/atom:feed/atom:entry", nsmgr)) {
            outputDocument.DocumentElement.AppendChild(outputDocument.ImportNode(entry, true));
          }
        }

        if (nextUri == null)
          break;
      }

      return outputDocument;
    }

    private static void FilterEntries(XmlDocument document, HashSet<string> categoriesToFilter, bool exclude)
    {
      var include = !exclude;
      var nsmgr = new XmlNamespaceManager(document.NameTable);

      nsmgr.AddNamespace("atom", Namespaces.Atom);

      foreach (var entry in document.SelectNodes("/atom:feed/atom:entry", nsmgr).Cast<XmlElement>().ToList()) {
        var categories = new HashSet<string>(entry.GetNodeValuesOf("atom:category/@term", nsmgr), StringComparer.Ordinal);

        if (exclude && categories.Overlaps(categoriesToFilter))
          entry.RemoveSelf();
        else if (include && !categories.Overlaps(categoriesToFilter))
          entry.RemoveSelf();
      }
    }

    private static void SaveAsMovableType(XmlDocument document, Stream outputStream, string blogId, bool retrieveComments)
    {
      /*
       * http://www.movabletype.jp/documentation/appendices/import-export-format.html
       */
      var writer = new StreamWriter(outputStream, Encoding.UTF8);

      writer.NewLine = "\n"; // ???

      var nsmgr = new XmlNamespaceManager(document.NameTable);

      nsmgr.AddNamespace("atom", Namespaces.Atom);
      nsmgr.AddNamespace("app", Namespaces.App);
      nsmgr.AddNamespace("hatena", Namespaces.Hatena);

      var entryRootLocation = string.Concat("http://", blogId, "/entry/");

      foreach (XmlNode entry in document.SelectNodes("/atom:feed/atom:entry", nsmgr)) {
        /*
         * metadata seciton
         */
        writer.WriteLine(string.Concat("AUTHOR: ", entry.GetSingleNodeValueOf("atom:author/atom:name/text()", nsmgr)));
        writer.WriteLine(string.Concat("TITLE: ", entry.GetSingleNodeValueOf("atom:title/text()", nsmgr)));

        var entryLocation = entry.GetSingleNodeValueOf("atom:link[@rel='alternate' and @type='text/html']/@href", nsmgr);

        if (entryLocation.StartsWith(entryRootLocation, StringComparison.Ordinal))
          writer.WriteLine(string.Concat("BASENAME: ", entryLocation.Substring(entryRootLocation.Length)));

        writer.WriteLine(string.Concat("STATUS: ", entry.GetSingleNodeValueOf("app:control/app:draft/text()", nsmgr) == "yes" ? "Draft" : "Publish"));
        writer.WriteLine("CONVERT BREAKS: 0");

        var updatedDate = DateTimeOffset.Parse(entry.GetSingleNodeValueOf("atom:updated/text()", nsmgr));

        writer.WriteLine(string.Concat("DATE: ", ToMovableTypeDateString(updatedDate.LocalDateTime)));

        var tags = entry.GetNodeValuesOf("atom:category/@term", nsmgr)
                        .Select(tag => tag.Contains(" ") ? string.Concat("\"", tag, "\"") : tag);

        writer.WriteLine(string.Concat("TAGS: ", string.Join(",", tags)));

        /*
         * multiline field seciton
         */
        const string multilineFieldDelimiter = "-----";

        writer.WriteLine(multilineFieldDelimiter);

        writer.WriteLine("BODY:");
        //writer.WriteLine(entry.GetSingleNodeValueOf("atom:content/text()", nsmgr));
        writer.WriteLine( entry.GetSingleNodeValueOf("hatena:formatted-content/text()", nsmgr));
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

    private static string ToMovableTypeDateString(DateTime dateTime)
    {
      return dateTime.ToString("MM/dd/yyyy hh\\:mm\\:ss tt", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void SaveAsHatenaDiary(XmlDocument document, Stream outputStream, bool retrieveComments)
    {
      var nsmgr = new XmlNamespaceManager(document.NameTable);

      nsmgr.AddNamespace("atom", Namespaces.Atom);
      nsmgr.AddNamespace("app", Namespaces.App);
      nsmgr.AddNamespace("hatena", Namespaces.Hatena);

      var outputDocument = new XmlDocument();

      outputDocument.AppendChild(outputDocument.CreateXmlDeclaration("1.0", "UTF-8", null));

      var diaryElement = outputDocument.AppendChild(outputDocument.CreateElement("diary"));
      var dayElements = new Dictionary<string, XmlElement>();

      foreach (XmlNode entry in document.SelectNodes("/atom:feed/atom:entry", nsmgr)) {
        var updatedDate = DateTimeOffset.Parse(entry.GetSingleNodeValueOf("atom:updated/text()", nsmgr));
        var date = updatedDate.ToLocalTime().DateTime.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        XmlElement dayElement, bodyElement;

        if (dayElements.TryGetValue(date, out dayElement)) {
          bodyElement = (XmlElement)dayElement.FirstChild;
        }
        else {
          dayElement = diaryElement.AppendElement("day");

          dayElement.SetAttribute("date", date);
          dayElement.SetAttribute("title", string.Empty);

          bodyElement = dayElement.AppendElement("body");

          dayElements[date] = dayElement;
        }

        var body = new StringBuilder();

        body.AppendFormat("*{0}*", UnixTimeStamp.ToInt64(updatedDate));

        var joinedCategory = string.Join("][", entry.GetNodeValuesOf("atom:category/@term", nsmgr));

        if (0 < joinedCategory.Length)
          body.AppendFormat("[{0}]", joinedCategory);

        body.AppendLine(entry.GetSingleNodeValueOf("atom:title/text()", nsmgr));

        body.AppendLine(entry.GetSingleNodeValueOf("atom:content/text()", nsmgr));
        body.AppendLine();

        bodyElement.AppendText(body.ToString());

#if RETRIEVE_COMMENTS
        if (retrieveComments) {
          var entryUrl = entry.GetSingleNodeValueOf("atom:link[@rel='alternate' and @type='text/html']/@href", nsmgr);
          var commentsElement = dayElement.AppendElement("comments");

          foreach (var comment in RetrieveComments(entryUrl)) {
            var commentElement = commentsElement.AppendElement("comment");

            commentElement.AppendElement("username").AppendText(comment.Author);
            commentElement.AppendElement("body").AppendText(comment.Content);
            commentElement.AppendElement("timestamp").AppendText(XmlConvert.ToString(UnixTimeStamp.ToInt64(comment.Date)));
          }
        }
#endif
      }

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
              comment.Date = UnixTimeStamp.ToLocalDateTime(commentChildElement.GetSingleNodeValueOf("time/@data-epoch", long.Parse) / 1000);
              break;
          }
        }

        yield return comment;
      }

      Console.Error.WriteLine("完了");
    }
#endif

    private static void Usage(string format, params string[] args)
    {
      if (format != null) {
        Console.Error.Write("error: ");
        Console.Error.WriteLine(format, args);
        Console.Error.WriteLine();
      }

      var assm = Assembly.GetEntryAssembly();
      var version = (assm.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)[0] as AssemblyInformationalVersionAttribute).InformationalVersion;

      Console.Error.WriteLine("{0} version {1}", assm.GetName().Name, version);
      Console.Error.WriteLine("usage:");
      Console.Error.WriteLine("  {0} -id <hatena-id> -blogid <blog-id> -apikey <api-key> [-format (hatena|mt)] [outfile]",
                              System.IO.Path.GetFileName(assm.Location));

      Console.Error.WriteLine("options:");
#if RETRIEVE_COMMENTS
      Console.Error.WriteLine("  -comment : dump comments posted on entry");
#endif
      Console.Error.WriteLine("  -excat <category> : category to be excluded");
      Console.Error.WriteLine("  -incat <category> : category to be included");

      Environment.Exit(-1);
    }
  }
}
