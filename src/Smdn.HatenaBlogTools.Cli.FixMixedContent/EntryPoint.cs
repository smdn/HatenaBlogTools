// SPDX-FileCopyrightText: 2018 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

namespace Smdn.HatenaBlogTools;

public partial class FixMixedContent {
  private static int Main(string[] args)
  {
    try {
      new FixMixedContent().Run(args);

      return 0;
    }
    catch (AbortCommandException) {
      return -1;
    }
  }
}
