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
using System.Xml.Linq;

using Smdn.HatenaBlogTools.HatenaBlog;

namespace Smdn.HatenaBlogTools {
  partial class Login : CliBase {
    protected override string GetDescription() => "AtomPubによるはてなブログへのログインを行います。　記事の変更等は行いません。";

    protected override string GetUsageExtraMandatoryOptions() => string.Empty;

    protected override IEnumerable<string> GetUsageExtraOptionDescriptions()
    {
      yield return "-v, --verbose   : ログインに成功した場合、レスポンスのサービス文書を表示します。";
    }

    public void Run(string[] args)
    {
      if (!ParseCommonCommandLineArgs(ref args, out var credential))
        return;

      bool verbose = false;

      for (var i = 0; i < args.Length; i++) {
        switch (args[i]) {
          case "--verbose":
          case "-v":
            verbose = true;
            break;
        }
      }

      var hatenaBlog = CreateClient(credential);

      var statusCode = hatenaBlog.Login(out var serviceDocument);

      if (statusCode == HttpStatusCode.OK) {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("ログインに成功しました。");
        Console.ResetColor();

        Console.WriteLine("はてなID: {0}", hatenaBlog.HatenaId);
        Console.WriteLine("ブログID: {0}", hatenaBlog.BlogId);
        Console.WriteLine("ブログタイトル: {0}", hatenaBlog.BlogTitle);
        Console.WriteLine("コレクションURI: {0}", hatenaBlog.CollectionUri);
      }
      else {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine("ログインに失敗しました。　({0:D} {0})", statusCode);
        Console.ResetColor();
      }

      if (verbose && serviceDocument != null) {
        Console.WriteLine();
        Console.WriteLine(serviceDocument);
        Console.WriteLine();
      }
    }
  }
}
