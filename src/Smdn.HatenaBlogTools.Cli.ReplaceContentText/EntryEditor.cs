// SPDX-FileCopyrightText: 2014 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Smdn.HatenaBlogTools.HatenaBlog;

namespace Smdn.HatenaBlogTools;

public class EntryEditor : IHatenaBlogEntryEditor {
  private readonly string replaceFrom;
  private readonly string replaceTo;
  private readonly EntryTextModifier entryTextModifier;

  public EntryEditor(string replaceFrom, string replaceTo, EntryTextModifier entryTextModifier)
  {
    this.replaceFrom = replaceFrom;
    this.replaceTo = replaceTo;
    this.entryTextModifier = entryTextModifier ?? throw new ArgumentNullException(nameof(entryTextModifier));
  }

  public bool Edit(Entry entry, out string originalText, out string modifiedText)
  {
    return entryTextModifier.Modify(
      entry: entry,
      modifier: original => original.Replace(replaceFrom, replaceTo),
      out originalText,
      out modifiedText
    );
  }
}
