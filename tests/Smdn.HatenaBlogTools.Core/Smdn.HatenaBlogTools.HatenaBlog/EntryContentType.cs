// SPDX-FileCopyrightText: 2022 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#nullable enable

using System;
using NUnit.Framework;

namespace Smdn.HatenaBlogTools.HatenaBlog;

[TestFixture]
public class EntryContentTypeTests {
  [TestCase("text/plain", ".txt")]
  [TestCase("TEXT/PLAIN", ".txt")]
  [TestCase("text/x-hatena-syntax", ".hatena.txt")]
  [TestCase("TEXT/X-HATENA-SYNTAX", ".hatena.txt")]
  [TestCase("text/x-markdown", ".md")]
  [TestCase("TEXT/X-MARKDOWN", ".md")]
  [TestCase("text/html", ".html")]
  [TestCase("TEXT/HTML", ".html")]
  [TestCase("text/x-unknown", null)]
  public void GetFileExtension(string contentType, string? expectedFileExtension)
    => Assert.That(EntryContentType.GetFileExtension(contentType), Is.EqualTo(expectedFileExtension));
}
