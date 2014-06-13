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
using System.IO;
using System.Net;
using System.Reflection;
using System.Xml;

using Smdn.Xml;

namespace Smdn.Applications.HatenaBlogTools {
  class MainClass {
    public static void Main(string[] args)
    {
      var entry = new Entry();
      string hatenaId = null;
      string blogId = null;
      string apiKey = null;
      string contentFile = null;

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
            entry.Title = args[++i];
            break;

          case "-category":
            entry.Categories.Add(args[++i]);
            break;

          case "-draft":
            entry.IsDraft = true;
            break;

          case "-fromfile":
            contentFile = args[++i];
            break;

          case "/help":
          case "-h":
          case "--help":
            Usage(null);
            break;

          default:
            entry.Content = args[i];
            break;
        }
      }

      if (string.IsNullOrEmpty(hatenaId))
        Usage("hatena-idを指定してください");

      if (string.IsNullOrEmpty(blogId))
        Usage("blog-idを指定してください");

      if (string.IsNullOrEmpty(apiKey))
        Usage("api-keyを指定してください");

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

      Console.Write("投稿しています ... ");

      var collectionUri = new Uri(string.Concat("http://blog.hatena.ne.jp/", hatenaId, "/", blogId, "/atom/entry"));
      var atom = new Atom();

      atom.Credential = new NetworkCredential(hatenaId, apiKey);

      HttpStatusCode statusCode;

      var responseDocument = HatenaBlog.PostEntry(atom, collectionUri, entry, out statusCode);

      if (statusCode == HttpStatusCode.Created) {
        var nsmgr = new XmlNamespaceManager(responseDocument.NameTable);

        nsmgr.AddNamespace("atom", Namespaces.Atom);

        Console.WriteLine("完了しました: {0}", responseDocument.GetSingleNodeValueOf("atom:entry/atom:link[@rel='alternate']/@href", nsmgr));
      }
      else {
        Console.Error.WriteLine("失敗しました: {0}", statusCode);
      }
    }

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
      Console.Error.WriteLine("  {0} -id <hatena-id> -blogid <blog-id> -apikey <api-key> -title <title> -category <category> <content>",
                              System.IO.Path.GetFileName(assm.Location));
      Console.Error.WriteLine("options:");
      Console.Error.WriteLine("  -draft : post entry as draft");
      Console.Error.WriteLine("  -fromfile <file>: post content from <file>");
      Console.Error.WriteLine("  -fromfile -: post content from stdin");

      Environment.Exit(-1);
    }
  }
}
