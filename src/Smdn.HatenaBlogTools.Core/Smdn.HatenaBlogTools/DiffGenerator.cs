// SPDX-FileCopyrightText: 2018 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.HatenaBlogTools;

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
