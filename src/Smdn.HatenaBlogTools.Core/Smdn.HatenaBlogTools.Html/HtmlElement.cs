// SPDX-FileCopyrightText: 2018 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Smdn.HatenaBlogTools.Html;

public class HtmlElement : HtmlNode {
  public string LocalName { get; }
  public IReadOnlyList<HtmlAttribute> Attributes { get; }

  internal Match Match { get; }
  private readonly string elementClose;

  internal HtmlElement(string localName, Match match, IReadOnlyList<HtmlAttribute> attributes, string elementClose)
  {
    LocalName = localName;
    Match = match;
    Attributes = attributes;
    this.elementClose = elementClose;
  }

  public override string ConstructHtml()
  {
    if (Attributes.Count == 0)
      return Match.Value;

    var sb = new StringBuilder(1024);

    sb.Append('<');
    sb.Append(LocalName);

    foreach (var attr in Attributes) {
      sb.Append(attr.ConstructHtml());
    }

    sb.Append(elementClose);

    return sb.ToString();
  }
}
