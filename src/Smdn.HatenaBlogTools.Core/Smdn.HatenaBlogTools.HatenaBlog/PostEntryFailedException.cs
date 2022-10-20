// SPDX-FileCopyrightText: 2013 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.HatenaBlogTools.HatenaBlog;

public class PostEntryFailedException : Exception {
  public Entry CausedEntry { get; }

  public PostEntryFailedException(Entry causedEntry, Exception innerException)
    : base("exception occured while posting entry", innerException)
  {
    CausedEntry = causedEntry;
  }
}
