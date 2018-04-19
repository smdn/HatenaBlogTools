//
// Author:
//       smdn <smdn@smdn.jp>
//
// Copyright (c) 2018 smdn
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
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Smdn.Applications.HatenaBlogTools {
  public interface IDiffGenerator {
    void DisplayDifference(string originalText, string modifiedText);
  }

  public static class DiffGenerator {
    private class NullDiffGenerator : IDiffGenerator {
      public void DisplayDifference(string originalText, string modifiedText)
      {
        // do nothing
      }
    }

    private class DefaultDiffGenerator : IDiffGenerator {
      private readonly string descriptionForOriginalText;
      private readonly string descriptionForModifiedText;

      public DefaultDiffGenerator(string descriptionForOriginalText,
                                  string descriptionForModifiedText)
      {
        this.descriptionForOriginalText = descriptionForOriginalText;
        this.descriptionForModifiedText = descriptionForModifiedText;
      }

      public bool IsAvailable() => true;

      public void DisplayDifference(string originalText, string modifiedText)
      {
        Console.WriteLine($"[  {descriptionForOriginalText}  ]");
        Console.WriteLine(originalText);
        Console.WriteLine($"[  {descriptionForModifiedText}  ]");
        Console.WriteLine(modifiedText);
        Console.WriteLine();
      }
    }

    public static IDiffGenerator Create(bool silent,
                                        string command,
                                        string commandArgs,
                                        string descriptionForOriginalText,
                                        string descriptionForModifiedText)
    {
      if (silent)
        return new NullDiffGenerator();

      if (string.IsNullOrEmpty(command))
        return new DefaultDiffGenerator(descriptionForOriginalText,
                                        descriptionForModifiedText);

      return new DiffCommand(command, commandArgs);
    }

    private static readonly string[] testOriginalTextLines = new[] {
      "diffコマンドのテストです",
      "この行の差分が見えていなければ失敗です",
      "diffコマンドのテストです",
    };

    private static readonly string[] testModifiedTextLines = new[] {
      "diffコマンドのテストです",
      "この行の差分が見えていれば成功です",
      "diffコマンドのテストです",
    };

    public static void Test(IDiffGenerator generator)
    {
      generator.DisplayDifference(string.Join(Environment.NewLine, testOriginalTextLines),
                                  string.Join(Environment.NewLine, testModifiedTextLines));
    }
  }

  public class DiffCommand : IDiffGenerator {
    private static readonly Encoding utf8EncodingNoBom = new UTF8Encoding(false);

    private static readonly string temporaryDirectoryPath = "./.tmp/";
    private static readonly string temporaryFilePathOriginal = temporaryDirectoryPath + "original.txt";
    private static readonly string temporaryFilePathModified = temporaryDirectoryPath + "modified.txt";

    private readonly string command;
    private readonly string commandArgs;

    public DiffCommand(string command, string commandArgs)
    {
      this.command = command;
      this.commandArgs = commandArgs;
    }

    public void DisplayDifference(string originalText, string modifiedText)
    {
      Diff(() => Console.OpenStandardOutput(),
           () => Console.OpenStandardError(),
           originalText,
           modifiedText);

      Console.WriteLine(); // XXX
    }

    private void Diff(Func<Stream> openStdout, Func<Stream> openStderr, string originalText, string modifiedText)
    {
      try {
        Directory.CreateDirectory(temporaryDirectoryPath);

        File.WriteAllText(temporaryFilePathOriginal, originalText + Environment.NewLine, utf8EncodingNoBom);
        File.WriteAllText(temporaryFilePathModified, modifiedText + Environment.NewLine, utf8EncodingNoBom);

        var arguments = $"{commandArgs} '{temporaryFilePathOriginal}' '{temporaryFilePathModified}'";

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
          using (var stdout = openStdout()) {
            process.StandardOutput.BaseStream.CopyTo(stdout);
          }

          using (var stderr = openStderr()) {
            process.StandardError.BaseStream.CopyTo(stderr);
          }

          process.WaitForExit();
        }
      }
      finally {
        if (Directory.Exists(temporaryDirectoryPath))
          Directory.Delete(temporaryDirectoryPath, true);
      }
    }
  }
}