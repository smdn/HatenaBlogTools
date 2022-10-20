// SPDX-FileCopyrightText: 2018 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

namespace Smdn.HatenaBlogTools;

internal partial class PostNewEntry {
  private static int Main(string[] args)
  {
    try {
      new PostNewEntry().Run(args);

      return 0;
    }
    catch (AbortCommandException) {
      return -1;
    }
  }
}
