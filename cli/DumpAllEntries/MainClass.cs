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

    public static void Main(string[] args)
    {
      string hatenaId = null;
      string blogId = null;
      string apiKey = null;
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

      var atom = new Atom();

      atom.Credential = new NetworkCredential(hatenaId, apiKey);

      var rootEndPoint = new Uri(string.Concat("http://blog.hatena.ne.jp/", hatenaId, "/", blogId, "/atom/"));
      var nextUri = new Uri(rootEndPoint, "./entry");
      HttpStatusCode statusCode;
      XmlDocument outputDocument = null;

      for (;;) {
        var collectionDocument = atom.Get(nextUri, out statusCode);

        if (statusCode != HttpStatusCode.OK) {
          Console.Error.WriteLine("中断しました ({0})", statusCode);
          return;
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

      // 結果を保存
      using (var outputStream = outputFile == "-"
             ? Console.OpenStandardOutput()
             : new FileStream(outputFile, FileMode.Create, FileAccess.Write)) {

        switch (outputFormat) {
          case OutputFormat.HatenaDiary:
            SaveAsHatenaDiary(outputDocument, outputStream);
            break;

          case OutputFormat.MovableType:
            SaveAsMovableType(outputDocument, outputStream);
            break;

          case OutputFormat.Default:
          default:
            outputDocument.Save(outputStream);
            break;
        }
      }
    }

    private static void SaveAsMovableType(XmlDocument document, Stream outputStream)
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

      foreach (XmlNode entry in document.SelectNodes("/atom:feed/atom:entry", nsmgr)) {
        /*
         * metadata seciton
         */
        writer.WriteLine(string.Concat("AUTHOR: ", entry.GetSingleNodeValueOf("atom:author/atom:name/text()", nsmgr)));
        writer.WriteLine(string.Concat("TITLE: ", entry.GetSingleNodeValueOf("atom:title/text()", nsmgr)));
        writer.WriteLine(string.Concat("STATUS: ", entry.GetSingleNodeValueOf("app:control/app:draft/text()", nsmgr) == "yes" ? "Draft" : "Publish"));

        var updatedDate = DateTimeOffset.Parse(entry.GetSingleNodeValueOf("atom:updated/text()", nsmgr));

        writer.WriteLine(string.Concat("DATE: ", updatedDate.ToString("MM/dd/yyyy hh\\:mm\\:ss tt", System.Globalization.CultureInfo.InvariantCulture)));

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

        // end of entry
        const string entryDelimiter = "--------";

        writer.WriteLine(entryDelimiter);
      }

      writer.Flush();
    }

    private static void SaveAsHatenaDiary(XmlDocument document, Stream outputStream)
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
          dayElement = (XmlElement)diaryElement.AppendChild(outputDocument.CreateElement("day"));

          dayElement.SetAttribute("date", date);
          dayElement.SetAttribute("title", string.Empty);

          bodyElement = (XmlElement)dayElement.AppendChild(outputDocument.CreateElement("body"));

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
      }

      outputDocument.Save(outputStream);
    }

    private static void Usage(string format, params string[] args)
    {
      if (format != null) {
        Console.Error.Write("error: ");
        Console.Error.WriteLine(format, args);
      }

      Console.Error.WriteLine("usage:");
      Console.Error.WriteLine("  {0} -id <hatena-id> -blogid <blog-id> -apikey <api-key> [-format (hatena|mt)] [outfile]",
                              System.IO.Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location));

      Environment.Exit(-1);
    }
  }
}
