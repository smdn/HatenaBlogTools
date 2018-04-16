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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Smdn.Xml;

namespace Smdn.Applications.HatenaBlogTools {
  partial class MainClass {
    private static string GetUsageExtraMandatoryOptions() => "-from 'oldtext' [-to 'newtext']";

    private static IEnumerable<string> GetUsageExtraOptionDescriptions()
    {
      yield return "-from <oldtext>       : text to be replaced";
      yield return "-to <newtext>         : text to replace <oldtext>";
      yield return "-regex                : treat <oldtext> and <newtext> as regular expressions";
      yield return "-diff-cmd <command>   : use <command> as diff command";
      yield return "-diff-cmd-args <args> : specify arguments for diff command";
      yield return "-v                    : display replacement result";
      yield return "-n                    : dry run";
    }

    public static void Main(string[] args)
    {
      HatenaBlogAtomPub.InitializeHttpsServicePoint();

      if (!ParseCommonCommandLineArgs(ref args, out HatenaBlogAtomPub hatenaBlog))
        return;

      string replaceFromText = null;
      string replaceToText = null;
      bool replaceAsRegex = false;
      string diffCommand = null;
      string diffCommandArgs = null;
      bool verbose = false;
      bool dryrun = false;

      for (var i = 0; i < args.Length; i++) {
        switch (args[i]) {
          case "-from":
            replaceFromText = args[++i];
            break;

          case "-to":
            replaceToText = args[++i];
            break;

          case "-regex":
            replaceAsRegex = true;
            break;

          case "-diff-cmd":
            diffCommand = args[++i];
            break;

          case "-diff-cmd-args":
            diffCommandArgs = args[++i];
            break;

          case "-v":
            verbose = true;
            break;

          case "-n":
            dryrun = true;
            break;
        }
      }

      if (string.IsNullOrEmpty(replaceFromText))
        Usage("置換する文字列を指定してください");

      if (replaceToText == null)
        replaceToText = string.Empty; // delete

      var regex = replaceAsRegex ? new Regex(replaceFromText, RegexOptions.Multiline) : null;
      var replace = new Func<string, string>(input => {
        if (input == null)
          return null;

        if (regex == null)
          return input.Replace(replaceFromText, replaceToText);
        else
          return regex.Replace(input, replaceToText);
      });

      var showDiff = verbose ? new Action<string, string>((textOld, textNew) => {
        if (string.IsNullOrEmpty(diffCommand))
          ShowDiff(textOld, textNew);
        else
          ShowDiffWithCommand(diffCommand, diffCommandArgs, textOld, textNew);
      }) : null;

      if (!Login(hatenaBlog))
        return;

      ReplaceContentText(hatenaBlog, verbose, dryrun, replace, showDiff);
    }

    private static void ReplaceContentText(HatenaBlogAtomPub hatenaBlog,
                                           bool verbose,
                                           bool dryrun,
                                           Func<string, string> replace,
                                           Action<string, string> showDiff)
    {
      foreach (var entry in hatenaBlog.EnumerateEntries()) {
        var newContent = replace(entry.Content);

        Console.Write("{0} \"{1}\" ", entry.EntryUri, entry.Title);

        if (string.Equals(entry.Content, newContent, StringComparison.Ordinal)) {
          Console.WriteLine("(該当個所なし)");
        }
        else {
          if (showDiff != null) {
            Console.WriteLine("以下の内容に置換します");

            showDiff(entry.Content, newContent);

            Console.WriteLine();
          }

          if (dryrun)
            continue;

          Console.Write("{0} \"{1}\" を更新中 ... ", entry.EntryUri, entry.Title);

          entry.Content = newContent;

          var statusCode = hatenaBlog.UpdateEntry(entry, out _);

          if (statusCode == HttpStatusCode.OK) {
            hatenaBlog.WaitForCinnamon();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("更新しました");
            Console.ResetColor();
          }
          else {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("更新に失敗しました: {0}", statusCode);
            Console.ResetColor();
          }
        }
      }
    }

    private static void ShowDiff(string textOld, string textNew)
    {
      Console.WriteLine("[  現在の本文  ]");
      Console.WriteLine(textOld);
      Console.WriteLine("[  置換後の本文  ]");
      Console.WriteLine(textNew);
    }

    private static readonly Encoding utf8nobom = new UTF8Encoding(false);

    private static void ShowDiffWithCommand(string command, string arguments, string textOld, string textNew)
    {
      const string dirTemp = "./.tmp/";
      const string fileOld = dirTemp + "current.txt";
      const string fileNew = dirTemp + "replace.txt";

      try {
        Directory.CreateDirectory(dirTemp);

        File.WriteAllText(fileOld + Environment.NewLine, textOld, utf8nobom);
        File.WriteAllText(fileNew + Environment.NewLine, textNew, utf8nobom);

        arguments = $"'{fileOld}' '{fileNew}' {arguments}";

        ProcessStartInfo psi;

        if (File.Exists("/bin/sh")) { // XXX: for unix
          if (arguments != null)
            arguments = arguments.Replace("\"", "\\\"");

          psi = new ProcessStartInfo("/bin/sh", string.Format("-c \"{0} {1}\"", command, arguments));
        }
        else { // for windows
          psi = new ProcessStartInfo("cmd", string.Format("/c {0} {1}", command, arguments));
          psi.CreateNoWindow = true;
        }

        psi.UseShellExecute = false;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;

        using (var process = Process.Start(psi)) {
          using (var stdout = Console.OpenStandardOutput()) {
            process.StandardOutput.BaseStream.CopyTo(stdout);
          }

          using (var stderr = Console.OpenStandardError()) {
            process.StandardError.BaseStream.CopyTo(stderr);
          }

          process.WaitForExit();
        }
      }
      finally {
        if (Directory.Exists(dirTemp))
          Directory.Delete(dirTemp, true);
      }
    }
  }
}
