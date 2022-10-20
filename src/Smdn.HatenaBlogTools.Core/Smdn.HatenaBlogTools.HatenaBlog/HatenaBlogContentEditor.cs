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
using System.Text.RegularExpressions;

using Smdn.HatenaBlogTools.Html;

namespace Smdn.HatenaBlogTools.HatenaBlog;

public class HatenaBlogContentEditor : HtmlDocument {
  public HatenaBlogContentEditor(string input)
    : base(input)
  {
  }

  private static readonly Regex regexReplaceAttributeReferenceToHttps = new Regex(@"(?:\s*)(?<scheme>http)(?:\://[^/]+/)", RegexOptions.Singleline | RegexOptions.CultureInvariant);

  private static string ReplaceSchemeToHttps(string input, Regex regex, ref bool modified)
  {
    for (; ; ) {
      var match = regex.Match(input);

      if (!match.Success)
        break;

      var groupScheme = match.Groups["scheme"];

      input = string.Concat(input.Substring(0, groupScheme.Index),
                            "https",
                            input.Substring(groupScheme.Index + groupScheme.Length));

      modified |= true;
    }

    return input;
  }

  public bool FixMixedContentReferences()
  {
    return FixMixedContentReferences(attribute => true);
  }

  public bool FixMixedContentReferences(Predicate<HtmlAttribute> targetPredicate)
  {
    if (targetPredicate == null)
      throw new ArgumentNullException(nameof(targetPredicate));

    var modified = false;

    foreach (var element in Elements) {
      IEnumerable<HtmlAttribute> targets = null;

      switch (element.LocalName.ToLowerInvariant()) {
        // img@src
        // img@srcset
        // source@src
        // source@srcset
        case "img":
        case "source":
          targets = element.Attributes
                           .Where(a => a.IsNameEqualsTo("src") || a.IsNameEqualsTo("srcset"));
          break;

        // script@src
        // video@src
        // audio@src
        // iframe@src
        // embed@src
        case "script":
        case "video":
        case "audio":
        case "iframe":
        case "embed":
          targets = element.Attributes
                           .Where(a => a.IsNameEqualsTo("src"));
          break;

        // link[@rel="stylesheet"]@href
        case "link":
          if (element.Attributes.Any(a => a.IsNameEqualsTo("rel") && string.Equals(a.Value, "stylesheet", StringComparison.OrdinalIgnoreCase))) {
            // if rel=stylesheet then
            targets = element.Attributes
                             .Where(a => a.IsNameEqualsTo("href"));
          }
          break;

        // form@action
        case "form":
          targets = element.Attributes
                           .Where(a => a.IsNameEqualsTo("action"));
          break;

        // object@data
        case "object":
          targets = element.Attributes
                           .Where(a => a.IsNameEqualsTo("data"));
          break;

        default:
          break; // do nothing
      }

      if (targets == null)
        continue;

      foreach (var target in targets) {
        if (targetPredicate(target))
          target.Value = ReplaceSchemeToHttps(target.Value, regexReplaceAttributeReferenceToHttps, ref modified);
      }
    } // for each element

    return modified;
  } // end of method

  /// <summary>
  /// replaces the blog url in plain text content and html a@href to https
  /// </summary>
  public bool ReplaceBlogUrlToHttps(IEnumerable<string> hostNames)
  {
    var modified = false;

    foreach (var hostName in hostNames) {
      var regexReplaceBlogUrlToHttps = new Regex(@"(?<scheme>http)\://" + Regex.Escape(hostName) + "/", RegexOptions.Singleline | RegexOptions.CultureInvariant);

      foreach (var text in Texts) {
        text.Text = ReplaceSchemeToHttps(text.Text, regexReplaceBlogUrlToHttps, ref modified);
      }

      foreach (var anchor in Elements.Where(e => string.Equals(e.LocalName, "a", StringComparison.OrdinalIgnoreCase))) {
        foreach (var href in anchor.Attributes.Where(a => a.IsNameEqualsTo("href"))) {
          href.Value = ReplaceSchemeToHttps(href.Value, regexReplaceBlogUrlToHttps, ref modified);
        }
      }
    }

    return modified;
  }
}
