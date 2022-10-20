// SPDX-FileCopyrightText: 2018 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Smdn.HatenaBlogTools.Html;

public class HtmlDocument {
  private readonly List<HtmlNode> nodes;

  private readonly List<HtmlElement> elements;
  public IReadOnlyList<HtmlElement> Elements => elements;

  private readonly List<HtmlText> texts;
  public IReadOnlyList<HtmlText> Texts => texts;

  public HtmlDocument(string input)
  {
    nodes = new List<HtmlNode>();
    elements = HtmlParser.EnumerateHtmlElementStart(input).ToList();

    var endIndexOfLastElementStart = 0;

    foreach (var element in elements) {
      nodes.Add(new HtmlText(input.Substring(endIndexOfLastElementStart, element.Match.Index - endIndexOfLastElementStart)));
      nodes.Add(element);

      endIndexOfLastElementStart = element.Match.Index + element.Match.Length;
    }

    nodes.Add(new HtmlText(input.Substring(endIndexOfLastElementStart)));

    texts = nodes.OfType<HtmlText>().ToList();
  }

  public override string ToString()
  {
    var sb = new StringBuilder(10240);

    foreach (var node in nodes) {
      sb.Append(node.ConstructHtml());
    }

    return sb.ToString();
  }
}
