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

public class EntryTitleModifier : EntryTextModifier {
  public override bool Modify(
    Entry entry,
    Converter<string, string> modifier,
    out string originalText,
    out string modifiedText
  )
  {
    if (Modify(entry.Title, modifier, out originalText, out modifiedText)) {
      entry.Title = modifiedText;
      return true;
    }

    return false;
  }
}

public class EntryContentModifier : EntryTextModifier {
  public override bool Modify(
    Entry entry,
    Converter<string, string> modifier,
    out string originalText,
    out string modifiedText
  )
  {
    if (Modify(entry.Content, modifier, out originalText, out modifiedText)) {
      entry.Content = modifiedText;
      return true;
    }

    return false;
  }
}
