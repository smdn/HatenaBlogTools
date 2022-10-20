// SPDX-FileCopyrightText: 2013 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.Xml.Linq;

using Smdn.HatenaBlogTools.AtomPublishingProtocol;
using Smdn.Xml.Linq;

namespace Smdn.HatenaBlogTools.HatenaBlog;

public static class EntryContentType {
  public static readonly string Default = "text/plain";
  public static readonly string HatenaSyntax = "text/x-hatena-syntax";
  public static readonly string Markdown = "text/x-markdown";
  public static readonly string Html = "text/html";

  public static string GetFileExtension(string contentType)
  {
    if (string.Equals(contentType, HatenaSyntax, StringComparison.OrdinalIgnoreCase))
      return ".hatena.txt";
    if (string.Equals(contentType, Markdown, StringComparison.OrdinalIgnoreCase))
      return ".md";
    if (string.Equals(contentType, Html, StringComparison.OrdinalIgnoreCase))
      return ".html";
    if (string.Equals(contentType, Default, StringComparison.OrdinalIgnoreCase))
      return ".txt";

    return null;
  }
}
