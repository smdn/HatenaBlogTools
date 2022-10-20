
// SPDX-FileCopyrightText: 2014 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using System;
using System.Text.RegularExpressions;

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

public class RegexEntryEditor : IHatenaBlogEntryEditor {
  private readonly Regex regexToReplace;
  private readonly string replacement;
  private readonly EntryTextModifier entryTextModifier;

  public RegexEntryEditor(string regexToReplace, string replacement, EntryTextModifier entryTextModifier)
  {
    this.regexToReplace = new Regex(regexToReplace, RegexOptions.Multiline);
    this.replacement = replacement;
    this.entryTextModifier = entryTextModifier ?? throw new ArgumentNullException(nameof(entryTextModifier));
  }

  public bool Edit(Entry entry, out string originalText, out string modifiedText)
  {
    return entryTextModifier.Modify(
      entry: entry,
      modifier: original => regexToReplace.Replace(original, match => match.Result(replacement)),
      out originalText,
      out modifiedText
    );
  }
}
