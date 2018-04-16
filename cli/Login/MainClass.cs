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
      string hatenaId = null;
      string blogId = null;
      string apiKey = null;
      bool verbose = false;

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

          case "-v":
            verbose = true;
            break;

          case "/help":
          case "-h":
          case "--help":
            Usage(null);
            break;
        }
      }

      if (string.IsNullOrEmpty(hatenaId))
        Usage("hatena-idを指定してください");

      if (string.IsNullOrEmpty(blogId))
        Usage("blog-idを指定してください");

      if (string.IsNullOrEmpty(apiKey))
        Usage("api-keyを指定してください");

      var responseDocument = HatenaBlog.GetServiceDocuments(hatenaId, blogId, apiKey, out HttpStatusCode statusCode);

      if (statusCode == HttpStatusCode.OK) {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("ログインに成功しました。");
        Console.ResetColor();

        var nsmgr = new XmlNamespaceManager(responseDocument.NameTable);

        nsmgr.AddNamespace("app", Namespaces.App);
        nsmgr.AddNamespace("atom", Namespaces.Atom);

        Console.WriteLine("はてなID: {0}", hatenaId);
        Console.WriteLine("ブログID: {0}", blogId);
        Console.WriteLine("ブログタイトル: {0}", responseDocument.GetSingleNodeValueOf("/app:service/app:workspace/atom:title/text()", nsmgr));
      }
      else {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine("ログインに失敗しました。　({0:D} {0})", statusCode);
        Console.ResetColor();
      }

      if (verbose) {
        Console.WriteLine();

        using (var stdout = Console.OpenStandardOutput()) {
          var settings = new XmlWriterSettings();

          settings.NewLineChars = Environment.NewLine;
          settings.Indent = true;
          settings.IndentChars = " ";

          using (var writer = XmlWriter.Create(stdout, settings)) {
            responseDocument.Save(writer);
          }
        }

        Console.WriteLine();
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
      Console.Error.WriteLine("  {0} -id <hatena-id> -blogid <blog-id> -apikey <api-key>",
                              System.IO.Path.GetFileName(assm.Location));
      Console.Error.WriteLine("options:");
      Console.Error.WriteLine("  -v : display response document");

      Environment.Exit(-1);
    }
  }
}