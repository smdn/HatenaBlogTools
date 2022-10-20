// SPDX-FileCopyrightText: 2018 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
namespace Smdn.HatenaBlogTools;

public interface IDiffGenerator {
  void DisplayDifference(string originalText, string modifiedText);
}
