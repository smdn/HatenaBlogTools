// SPDX-FileCopyrightText: 2018 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
namespace Smdn.HatenaBlogTools.Html;

public class HtmlText : HtmlNode {
  public string Text { get; set; }

  public HtmlText(string text)
  {
    Text = text;
  }

  public override string ConstructHtml() => Text;
}
