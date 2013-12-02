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
      string title = null;
      string content = null;
      bool draft = false;
      var categories = new HashSet<string>(StringComparer.Ordinal);

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

          case "-title":
            title = args[++i];
            break;

          case "-category":
            categories.Add(args[++i]);
            break;

          case "-draft":
            draft = true;
            break;

          case "/help":
          case "-h":
          case "--help":
            Usage(null);
            break;

          default:
            content = args[i];
            break;
        }
      }

      if (string.IsNullOrEmpty(hatenaId))
        Usage("hatena-idを指定してください");

      if (string.IsNullOrEmpty(blogId))
        Usage("blog-idを指定してください");

      if (string.IsNullOrEmpty(apiKey))
        Usage("api-keyを指定してください");

      PostEntry(hatenaId, blogId, apiKey, title, categories, draft, content);
    }

    private static void Usage(string format, params string[] args)
    {
      if (format != null) {
        Console.Error.Write("error: ");
        Console.Error.WriteLine(format, args);
      }

      Console.Error.WriteLine("usage:");
      Console.Error.WriteLine("  {0} -id <hatena-id> -blogid <blog-id> -apikey <api-key> -title <title> -category <category> <content>",
        System.IO.Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location));
      Console.Error.WriteLine("options:");
      Console.Error.WriteLine("  -draft : post entry as draft");

      Environment.Exit(-1);
    }

    private static void PostEntry(string hatenaId, string blogId, string apiKey,
      string title, IEnumerable<string> categories, bool draft, string content)
    {
      var doc = new XmlDocument();

      doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));

      var e = doc.AppendElement("entry", Namespaces.Atom);

      e.SetAttribute("xmlns", Namespaces.Atom);
      e.SetAttribute("xmlns:app", Namespaces.App);

      e.AppendElement("title", Namespaces.Atom).AppendText(title);
      //e.AppendElement("updated", Namespaces.Atom).AppendText(entry.Updated);

      var c = e.AppendElement("content", Namespaces.Atom);

      c.SetAttribute("type", "text/plain");
      c.AppendText(content);

      foreach (var category in categories) {
        e.AppendElement("category", Namespaces.Atom).SetAttribute("term", category);
      }

      if (draft)
        e.AppendElement("control", Namespaces.App).AppendElement("draft", Namespaces.App).AppendText("yes");

      var atom = new Atom();
      var collectionUri = new Uri(string.Concat("http://blog.hatena.ne.jp/", hatenaId, "/", blogId, "/atom/entry"));

      atom.Credential = new NetworkCredential(hatenaId, apiKey);

      HttpStatusCode statusCode;

      var response = atom.Post(collectionUri, doc, out statusCode);

      if (statusCode == HttpStatusCode.Created) {
        var nsmgr = new XmlNamespaceManager(response.NameTable);

        nsmgr.AddNamespace("atom", Namespaces.Atom);

        Console.WriteLine("投稿が完了しました: {0}", response.GetSingleNodeValueOf("atom:entry/atom:link[@rel='alternate']/@href", nsmgr));
      }
      else {
        Console.Error.WriteLine("投稿に失敗しました: {0}", statusCode);
      }
    }
  }
}
