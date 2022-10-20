// SPDX-FileCopyrightText: 2018 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Smdn.HatenaBlogTools;

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

  private static readonly string temporaryDirectoryPath = Path.Combine(".", ".tmp");
  private static readonly string temporaryFilePathOriginal = Path.Combine(temporaryDirectoryPath, "original.txt");
  private static readonly string temporaryFilePathModified = Path.Combine(temporaryDirectoryPath, "modified.txt");

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
  }

  private void Diff(Func<Stream> openStdout, Func<Stream> openStderr, string originalText, string modifiedText)
  {
    try {
      Directory.CreateDirectory(temporaryDirectoryPath);

      File.WriteAllText(temporaryFilePathOriginal, originalText + Environment.NewLine, utf8EncodingNoBom);
      File.WriteAllText(temporaryFilePathModified, modifiedText + Environment.NewLine, utf8EncodingNoBom);

      ProcessStartInfo psi;

      if (File.Exists("/bin/sh")) { // XXX: for unix
        var arguments = commandArgs;

        if (arguments != null)
          arguments = arguments.Replace("\"", "\\\"");

        arguments = $"{arguments} '{temporaryFilePathOriginal}' '{temporaryFilePathModified}'";

        psi = new ProcessStartInfo("/bin/sh", string.Format("-c \"{0} {1}\"", command, arguments));
      }
      else { // for windows
        var arguments = $"{commandArgs} \"{Path.GetFullPath(temporaryFilePathOriginal)}\" \"{Path.GetFullPath(temporaryFilePathModified)}\"";

        //psi = new ProcessStartInfo("cmd", string.Format("/c \"{0}\" {1}", command, arguments));
        psi = new ProcessStartInfo(command, arguments);
        psi.CreateNoWindow = true;
      }

      psi.UseShellExecute = false;
      psi.RedirectStandardOutput = true;
      psi.RedirectStandardError = true;
      psi.StandardOutputEncoding = utf8EncodingNoBom;
      psi.StandardErrorEncoding = utf8EncodingNoBom;

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
