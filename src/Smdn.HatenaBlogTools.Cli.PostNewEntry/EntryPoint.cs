// SPDX-FileCopyrightText: 2018 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

namespace Smdn.HatenaBlogTools;

partial class PostNewEntry {
  static int Main(string[] args)
  {
    try {
      (new PostNewEntry()).Run(args);

      return 0;
    }
    catch (AbortCommandException) {
      return -1;
    }
  }
}
