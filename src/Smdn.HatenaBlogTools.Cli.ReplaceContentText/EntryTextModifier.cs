// SPDX-FileCopyrightText: 2020 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Smdn.HatenaBlogTools.HatenaBlog;

namespace Smdn.HatenaBlogTools;

public abstract class EntryTextModifier {
  public abstract bool Modify(
    Entry entry,
    Converter<string, string> modifier,
    out string originalText,
    out string modifiedText
  );

  protected bool Modify(
    string input,
    Converter<string, string> modifier,
    out string original,
    out string modified
  )
  {
    original = input;
    modified = modifier(input);

    return !(
      original.Length == modified.Length &&
      string.Equals(original, modified, StringComparison.Ordinal)
    );
  }
}
