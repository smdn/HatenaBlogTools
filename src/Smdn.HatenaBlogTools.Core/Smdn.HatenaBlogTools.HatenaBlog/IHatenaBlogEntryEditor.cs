// SPDX-FileCopyrightText: 2018 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
namespace Smdn.HatenaBlogTools.HatenaBlog;

public interface IHatenaBlogEntryEditor {
  bool Edit(Entry entry, out string originalText, out string modifiedText);
}
