// SPDX-FileCopyrightText: 2013 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Net;

namespace Smdn.HatenaBlogTools;

internal partial class Login : CliBase {
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
