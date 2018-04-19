// Author:
//       smdn <smdn@smdn.jp>
//
// Copyright (c) 2018 smdn
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Smdn.Applications.HatenaBlogTools {
  public class HtmlDocument {
    private readonly List<HtmlNode> nodes;

    private readonly List<HtmlElement> elements;
    public IReadOnlyList<HtmlElement> Elements => elements;

    private readonly List<HtmlText> texts;
    public IReadOnlyList<HtmlText> Texts => texts;

    public HtmlDocument(string input)
    {
      this.nodes = new List<HtmlNode>();
      this.elements = Html.EnumerateHtmlElementStart(input).ToList();

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
    public string LocalName { get; private set; }
    public IReadOnlyList<HtmlAttribute> Attributes { get; private set; }

    internal Match Match { get; private set; }
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

  public static class Html {
    private static readonly Regex regexElementStart = CreateElementStartRegex();

    private static Regex CreateElementStartRegex()
    {
      var whitespaceChars = @" \t\n\f\r";
      var whitespaceOneOrMore = $"[{whitespaceChars}]+";
      var whitespaceZeroOrMore = $"[{whitespaceChars}]*";

      var attributeNamePart = $"(?<attrname>[^{whitespaceChars}\"'>/=]+)";
      var attributeValueQuoted = "'(?<attrvalue>[^']*)'";
      var attributeValueDQuoted = "\"(?<attrvalue>[^\"]*)\"";
      var attributeValueNotQuoted = $"(?<attrvalue>[^{whitespaceChars}\"'`=><]+)";

      var attributeValuePart = $"({attributeValueDQuoted}|{attributeValueQuoted}|{attributeValueNotQuoted})";
      var attribute = $"(?<attr>{whitespaceOneOrMore}{attributeNamePart}({whitespaceZeroOrMore}={whitespaceZeroOrMore}{attributeValuePart})?)";
      var attributeList = $"{attribute}*";

      var elementLocalName = "[a-zA-z]+";

      return new Regex($"\\<(?<localname>{elementLocalName}){attributeList}(?<elementclose>{whitespaceZeroOrMore}/?\\>)",
                       RegexOptions.Multiline | RegexOptions.Compiled);
    }

    public static IEnumerable<HtmlElement> EnumerateHtmlElementStart(string input)
    {
      if (input == null)
        throw new ArgumentNullException(nameof(input));

      var match = regexElementStart.Match(input);

      while (match.Success) {
        var groupAttribute = match.Groups["attr"];
        var groupAttributeName = match.Groups["attrname"];
        var groupAttributeValue = match.Groups["attrvalue"];

        var attributes = new List<HtmlAttribute>(groupAttribute.Captures.Count);

        for (var attributeIndex = 0; attributeIndex < groupAttribute.Captures.Count; attributeIndex++) {
          var captureAttributeName = groupAttributeName.Captures[attributeIndex];
          var indexOfNextAtttributeStart = attributeIndex < groupAttributeName.Captures.Count - 1 ? groupAttributeName.Captures[attributeIndex + 1].Index : match.Index + match.Length;

          Capture captureAttributeValue = null;

          for (var captureIndex = 0; captureIndex < groupAttributeValue.Captures.Count; captureIndex++) {
            var capture = groupAttributeValue.Captures[captureIndex];

            if (captureAttributeName.Index + captureAttributeName.Length < capture.Index &&
                capture.Index + capture.Length < indexOfNextAtttributeStart) {
              captureAttributeValue = capture;
              break;
            }
          }

          var captureAttribute = groupAttribute.Captures[attributeIndex];

          attributes.Add(new HtmlAttribute(preamble: input.Substring(captureAttribute.Index, captureAttributeName.Index - captureAttribute.Index),
                                           captureName: captureAttributeName,
                                           delimiter: (captureAttributeValue == null) ? null : input.Substring(captureAttributeName.Index + captureAttributeName.Length, captureAttributeValue.Index - (captureAttributeName.Index + captureAttributeName.Length)),
                                           captureValue: captureAttributeValue,
                                           postabmle: (captureAttributeValue == null) ? null : input.Substring(captureAttributeValue.Index + captureAttributeValue.Length, (captureAttribute.Index + captureAttribute.Length) - (captureAttributeValue.Index + captureAttributeValue.Length))));
        }

        yield return new HtmlElement(match.Groups["localname"].Value,
                                     match,
                                     attributes,
                                     match.Groups["elementclose"].Value);

        match = match.NextMatch();
      }
    }
  }
}

