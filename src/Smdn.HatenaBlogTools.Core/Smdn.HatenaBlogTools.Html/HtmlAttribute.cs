// SPDX-FileCopyrightText: 2018 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Text.RegularExpressions;

namespace Smdn.HatenaBlogTools.Html;

#pragma warning disable CA1711
public class HtmlAttribute {
#pragma warning restore CA1711
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

  internal HtmlAttribute(string preamble, Capture captureName, string delimiter, Capture captureValue, string postamble)
  {
    this.preamble = preamble;
    this.captureName = captureName;
    this.delimiter = delimiter;
    this.captureValue = captureValue;
    this.postamble = postamble;

    Value = captureValue?.Value;
  }

  internal string ConstructHtml()
    => string.Concat(preamble, Name, delimiter, Value, postamble);

  public bool IsNameEqualsTo(string name)
    => string.Equals(Name, name, StringComparison.OrdinalIgnoreCase);
}
