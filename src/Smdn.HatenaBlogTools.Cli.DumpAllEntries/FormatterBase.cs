// SPDX-FileCopyrightText: 2020 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.IO;

using Smdn.HatenaBlogTools.HatenaBlog;

namespace Smdn.HatenaBlogTools;

public abstract class FormatterBase {
  public TimeZoneInfo EntryTimeZone { get; set; } = TimeZoneInfo.Local;

#if RETRIEVE_COMMENTS
  public FormatterBase(bool retrieveComments = false) // TODO
#else
  public FormatterBase()
#endif
  {
  }

  public abstract void Format(IEnumerable<PostedEntry> entries, Stream outputStream);

  public Stream ToStream(IEnumerable<PostedEntry> entries)
  {
    var stream = new MemoryStream();

    Format(entries, stream);

    stream.Position = 0L;

    return stream;
  }
}
