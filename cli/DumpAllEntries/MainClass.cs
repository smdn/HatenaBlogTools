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
using System.IO;
using System.Net;
using System.Xml;

using Smdn.Xml;

namespace Smdn.Applications.HatenaBlogTools {
  class MainClass {
    public static void Main(string[] args)
    {
      string hatenaId = null;
      string blogId = null;
      string apiKey = null;
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

      if (string.IsNullOrEmpty(hatenaId))
        Usage("blog-idを指定してください");

      if (string.IsNullOrEmpty(hatenaId))
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

        // atom:entryをコピーする
        if (outputDocument == null) {
          collectionDocument.SelectSingleNode("/atom:feed/atom:link[@rel='first']", nsmgr).RemoveSelf();
          collectionDocument.SelectSingleNode("/atom:feed/atom:link[@rel='next']", nsmgr).RemoveSelf();

          outputDocument = new XmlDocument();

          outputDocument.AppendChild(outputDocument.ImportNode(collectionDocument.DocumentElement, true));
        }
        else {
          foreach (XmlNode entry in collectionDocument.SelectNodes("/atom:feed/atom:entry", nsmgr)) {
            outputDocument.DocumentElement.AppendChild(outputDocument.ImportNode(entry, true));
          }
        }

        if (nextUri == null)
          break;
      }

      using (var outputStream = outputFile == "-"
             ? Console.OpenStandardOutput()
             : new FileStream(outputFile, FileMode.Create, FileAccess.Write)) {
        outputDocument.Save(outputStream);
      }
    }

    private static void Usage(string format, params string[] args)
    {
      if (format != null) {
        Console.Error.Write("error: ");
        Console.Error.WriteLine(format, args);
      }

      Console.Error.WriteLine("usage:");
      Console.Error.WriteLine("  {0} -id <hatena-id> -blogid <blog-id> -apikey <api-key> [outfile]",
                              System.IO.Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location));

      Environment.Exit(-1);
    }
  }
}
