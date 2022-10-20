// SPDX-FileCopyrightText: 2018 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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

public abstract class HtmlNode {
  public abstract string ConstructHtml();
}

public class HtmlText : HtmlNode {
  public string Text { get; set; }

  public HtmlText(string text)
  {
    this.Text = text;
  }

  public override string ConstructHtml() => Text;
}

public class HtmlElement : HtmlNode {
  public string LocalName { get; }
  public IReadOnlyList<HtmlAttribute> Attributes { get; }

  internal Match Match { get; }
  private readonly string elementClose;

  internal HtmlElement(string localName, Match match, List<HtmlAttribute> attributes, string elementClose)
  {
    this.LocalName = localName;
    this.Match = match;
    this.Attributes = attributes;
    this.elementClose = elementClose;
  }

  public override string ConstructHtml()
  {
    if (Attributes.Count == 0)
      return Match.Value;

    var sb = new StringBuilder(1024);

    sb.Append("<");
    sb.Append(LocalName);

    foreach (var attr in Attributes) {
      sb.Append(attr.ConstructHtml());
    }

    sb.Append(elementClose);

    return sb.ToString();
  }
}

public class HtmlAttribute {
  // <element ___attr1_=_'value'____attr2=...
  //          >  >    >   >    >>
  //          |  |    |   |    ||
  //          |  |    |   |    |next preamble
  //          |  |    |   |    postamble
  //          |  |    |   capture 'attrvalue' -> CaptureValue/Value
  //          |  |    delimiter
  //          |  capture 'attrname' -> CaptureName/Name
  //          preamble

  private readonly string preamble;
  private readonly Capture captureName;
  private readonly string delimiter;
  private readonly Capture captureValue;
  private readonly string postamble;

  public string Name => captureName?.Value;
  public string Value { get; set; }

  internal HtmlAttribute(string preamble, Capture captureName, string delimiter, Capture captureValue, string postabmle)
  {
    this.preamble = preamble;
    this.captureName = captureName;
    this.delimiter = delimiter;
    this.captureValue = captureValue;
    this.postamble = postabmle;

    this.Value = captureValue?.Value;
  }

  internal string ConstructHtml()
  {
    return string.Concat(preamble, Name, delimiter, Value, postamble);
  }

  public bool IsNameEqualsTo(string name)
  {
    return string.Equals(Name, name, StringComparison.OrdinalIgnoreCase);
  }
}
