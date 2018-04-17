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
using System.Linq;
using System.Net;
using System.Xml.Linq;

using Smdn.Xml.Linq;

namespace Smdn.Applications.HatenaBlogTools {
  partial class MainClass {
    private static string GetUsageExtraMandatoryOptions() => "-title <title> -category <category> <content>";

    private static IEnumerable<string> GetUsageExtraOptionDescriptions()
    {
      yield return "<content>            : content of new entry";
      yield return "-title <title>       : title of new entry";
      yield return "-category <category> : category of new entry";
      yield return "                       (ex: -category diary -category tech)";
      yield return "-draft               : post entry as draft";
      yield return "-fromfile <file>     : post entry content from <file>";
      yield return "-fromfile -          : post entry content from stdin";
    }

    public static void Main(string[] args)
    {
      HatenaBlogAtomPubClient.InitializeHttpsServicePoint();

      if (!ParseCommonCommandLineArgs(ref args, out HatenaBlogAtomPubClient hatenaBlog))
        return;

      var entry = new Entry();
      string contentFile = null;

      for (var i = 0; i < args.Length; i++) {
        switch (args[i]) {
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

          default:
            entry.Content = args[i];
            break;
        }
      }

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

      if (!Login(hatenaBlog))
        return;

      Console.Write("投稿しています ... ");

      var statusCode = hatenaBlog.PostEntry(entry, out XDocument responseDocument);

      if (statusCode == HttpStatusCode.Created) {
        var createdUri = responseDocument.Element(AtomPub.Namespaces.Atom + "entry")
                                         ?.Elements(AtomPub.Namespaces.Atom + "link")
                                         ?.FirstOrDefault(e => e.HasAttributeWithValue("rel", "alternate"))
                                         ?.GetAttributeValue("href");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("完了しました: {0}", createdUri);
        Console.ResetColor();
      }
      else {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine("失敗しました: {0}", statusCode);
        Console.ResetColor();
      }
    }
  }
}
