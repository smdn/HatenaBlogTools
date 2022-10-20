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
    this.nodes = new List<HtmlNode>();
    this.elements = HtmlParser.EnumerateHtmlElementStart(input).ToList();

    var endIndexOfLastElementStart = 0;

    foreach (var element in elements) {
      this.nodes.Add(new HtmlText(input.Substring(endIndexOfLastElementStart, element.Match.Index - endIndexOfLastElementStart)));
      this.nodes.Add(element);

      endIndexOfLastElementStart = element.Match.Index + element.Match.Length;
    }

    this.nodes.Add(new HtmlText(input.Substring(endIndexOfLastElementStart)));

    this.texts = this.nodes.OfType<HtmlText>().ToList();
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
