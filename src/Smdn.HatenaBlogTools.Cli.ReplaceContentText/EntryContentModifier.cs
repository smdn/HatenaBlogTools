// SPDX-FileCopyrightText: 2020 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Smdn.HatenaBlogTools.HatenaBlog;

namespace Smdn.HatenaBlogTools;

public class EntryContentModifier : EntryTextModifier {
  public override bool Modify(
    Entry entry,
    Converter<string, string> modifier,
    out string originalText,
    out string modifiedText
  )
  {
    if (EntryTextModifier.Modify(entry.Content, modifier, out originalText, out modifiedText)) {
      entry.Content = modifiedText;
      return true;
    }

    return false;
  }
}
