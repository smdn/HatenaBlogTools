// SPDX-FileCopyrightText: 2018 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

namespace Smdn.HatenaBlogTools;

partial class Login {
  static int Main(string[] args)
  {
    try {
      (new Login()).Run(args);

      return 0;
    }
    catch (AbortCommandException) {
      return -1;
    }
  }
}
