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
using System.Text.RegularExpressions;

namespace Smdn.Applications.HatenaBlogTools {
  public class HtmlElement {
    public Match Match { get; private set; }
    public string LocalName { get; private set; }
    public IReadOnlyList<HtmlAttribute> Attributes { get; private set; }

    public HtmlElement(Match match, string localName, IReadOnlyList<HtmlAttribute> attributes)
    {
      this.Match = match;
      this.LocalName = localName;
      this.Attributes = attributes;
    }
  }

  public class HtmlAttribute {
    public Capture CaptureName { get; private set; }
    public Capture CaptureValue { get; private set; }
    public string Name => CaptureName?.Value;
    public string Value => CaptureValue?.Value;

    public HtmlAttribute(Capture captureName, Capture captureValue)
    {
      this.CaptureName = captureName;
      this.CaptureValue = captureValue;
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

      return new Regex($"\\<(?<localname>{elementLocalName}){attributeList}{whitespaceZeroOrMore}/?\\>",
                       RegexOptions.Multiline | RegexOptions.Compiled);
    }

    public static IEnumerable<HtmlElement> EnumerateHtmlElementStart(string input)
    {
      if (input == null)
        throw new ArgumentNullException(nameof(input));

      var match = regexElementStart.Match(input);

      while (match.Success) {
        var attributes = new List<HtmlAttribute>(match.Groups["attr"].Captures.Count);

        var attributeNameGroup = match.Groups["attrname"];
        var attributeValueGroup = match.Groups["attrvalue"];

        for (var attributeIndex = 0; attributeIndex < attributeNameGroup.Captures.Count; attributeIndex++) {
          var attributeNameCapture = attributeNameGroup.Captures[attributeIndex];
          var indexOfNextAtttributeStart = attributeIndex < attributeNameGroup.Captures.Count - 1 ? attributeNameGroup.Captures[attributeIndex + 1].Index : match.Index + match.Length;

          Capture attributeValueCapture = null;

          for (var captureIndex = 0; captureIndex < attributeValueGroup.Captures.Count; captureIndex++) {
            var capture = attributeValueGroup.Captures[captureIndex];

            if (attributeNameCapture.Index + attributeNameCapture.Length < capture.Index &&
                capture.Index + capture.Length < indexOfNextAtttributeStart) {
              attributeValueCapture = capture;
              break;
            }
          }

          attributes.Add(new HtmlAttribute(attributeNameCapture, attributeValueCapture));
        }

        yield return new HtmlElement(match,
                                     match.Groups["localname"].Value,
                                     attributes);

        match = match.NextMatch();
      }
    }
  }
}

