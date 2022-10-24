// SPDX-FileCopyrightText: 2018 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Smdn.HatenaBlogTools;

public class DiffCommand : IDiffGenerator {
  private static readonly Encoding utf8EncodingNoBom = new UTF8Encoding(false);

  private static readonly string temporaryDirectoryPath = Path.Combine(".", ".tmp");
  private static readonly string temporaryFilePathOriginal = Path.Combine(temporaryDirectoryPath, "original.txt");
  private static readonly string temporaryFilePathModified = Path.Combine(temporaryDirectoryPath, "modified.txt");

  private readonly string command;
  private readonly string? commandArgs;

  public DiffCommand(string command, string? commandArgs)
  {
    this.command = command;
    this.commandArgs = commandArgs;
  }

  public void DisplayDifference(string originalText, string modifiedText)
  {
    Diff(
      () => Console.OpenStandardOutput(),
      () => Console.OpenStandardError(),
      originalText,
      modifiedText
    );
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

        psi = new ProcessStartInfo("/bin/sh", $"-c \"{command} {arguments}\"");
      }
      else { // for windows
        var arguments = $"{commandArgs} \"{Path.GetFullPath(temporaryFilePathOriginal)}\" \"{Path.GetFullPath(temporaryFilePathModified)}\"";

        // psi = new ProcessStartInfo("cmd", $"/c \"{command}\" {arguments}");
        psi = new ProcessStartInfo(command, arguments) {
          CreateNoWindow = true,
        };
      }

      psi.UseShellExecute = false;
      psi.RedirectStandardOutput = true;
      psi.RedirectStandardError = true;
      psi.StandardOutputEncoding = utf8EncodingNoBom;
      psi.StandardErrorEncoding = utf8EncodingNoBom;

      using var process = Process.Start(psi);

      if (process is null)
        throw new InvalidOperationException("new process could not be started.");

      using (var stdout = openStdout()) {
        process.StandardOutput.BaseStream.CopyTo(stdout);
      }

      using (var stderr = openStderr()) {
        process.StandardError.BaseStream.CopyTo(stderr);
      }

      if (!process.HasExited)
        process.WaitForExit();
    }
    finally {
      if (Directory.Exists(temporaryDirectoryPath))
        Directory.Delete(temporaryDirectoryPath, true);
    }
  }
}
