using System;
using System.Collections.Generic;
using System.IO;

using Smdn.Applications.HatenaBlogTools.HatenaBlog;

namespace Smdn.Applications.HatenaBlogTools {
  abstract class FormatterBase {
    public FormatterBase(
#if RETRIEVE_COMMENTS
      bool retrieveComments = false
#endif
    )
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
}
