//
// Author:
//       smdn <smdn@smdn.jp>
//
// Copyright (c) 2014 smdn
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
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

using Smdn.Xml;

namespace Smdn.Applications.HatenaBlogTools {
  class MainClass {
    public static void Main(string[] args)
    {
      string hatenaId = null;
      string blogId = null;
      string apiKey = null;
      string replaceFromText = null;
      string replaceToText = null;
      bool replaceAsRegex = false;
      bool verbose = false;
      bool dryrun = false;

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

          case "-from":
            replaceFromText = args[++i];
            break;

          case "-to":
            replaceToText = args[++i];
            break;

          case "-regex":
            replaceAsRegex = true;
            break;

          case "-v":
            verbose = true;
            break;

          case "-n":
            dryrun = true;
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

      if (string.IsNullOrEmpty(replaceFromText))
        Usage("置換する文字列を指定してください");

      if (replaceToText == null)
        replaceToText = string.Empty; // delete

      ReplaceContentText(hatenaId, blogId, apiKey, verbose, dryrun, delegate(string input) {
        if (input == null)
          return null;

        if (replaceAsRegex)
          return Regex.Replace(input, replaceFromText, replaceToText, RegexOptions.Multiline);
        else
          return input.Replace(replaceFromText, replaceToText);
      });
    }

    private static void ReplaceContentText(string hatenaId, string blogId, string apiKey,
                                             bool verbose, bool dryrun, Func<string, string> replace)
    {
      var atom = new Atom();

      atom.Credential = new NetworkCredential(hatenaId, apiKey);

      foreach (var entry in HatenaBlog.EnumerateEntries(hatenaId, blogId, apiKey)) {
        var newContent = replace(entry.Content);

        Console.Write("{0} \"{1}\" ", entry.MemberUri, entry.Title);

        if (string.Equals(entry.Content, newContent, StringComparison.Ordinal)) {
          Console.WriteLine("(該当個所なし)");
        }
        else {
          if (verbose) {
            Console.WriteLine("以下の内容に置換します");
            Console.WriteLine("[  現在の本文  ]");
            Console.WriteLine(entry.Content);
            Console.WriteLine("[  置換後の本文  ]");
            Console.WriteLine(newContent);
            Console.WriteLine();
          }

          if (dryrun)
            continue;

          Console.Write("更新中...");

          HttpStatusCode statusCode;

          entry.Content = newContent;

          HatenaBlog.UpdateEntry(atom, entry, out statusCode);

          if (statusCode == HttpStatusCode.OK) {
            HatenaBlog.WaitForCinnamon();
            Console.WriteLine("更新しました");
          }
          else {
            Console.Error.WriteLine("失敗しました: {0}", statusCode);
          }
        }
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
      Console.Error.WriteLine("  {0} -id <hatena-id> -blogid <blog-id> -apikey <api-key> -from 'oldtext' [-to 'newtext']",
                              System.IO.Path.GetFileName(assm.Location));

      Console.Error.WriteLine("options:");
      Console.Error.WriteLine("  -regex : use 'oldtext' and 'newtext' as regular expressions");
      Console.Error.WriteLine("  -v : display replacement result");
      Console.Error.WriteLine("  -n : dry run");

      Environment.Exit(-1);
    }
  }
}
