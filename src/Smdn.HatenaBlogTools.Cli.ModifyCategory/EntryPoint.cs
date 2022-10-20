// SPDX-FileCopyrightText: 2018 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

namespace Smdn.HatenaBlogTools;

partial class ModifyCategory {
  static int Main(string[] args)
  {
    try {
      (new ModifyCategory()).Run(args);

      return 0;
    }
    catch (AbortCommandException) {
      return -1;
    }
  }
}
