//
// Author:
//       smdn <smdn@smdn.jp>
//
// Copyright (c) 2020 smdn
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
using System.IO;
using System.Text;
using System.Xml.Linq;

using Smdn.Applications.HatenaBlogTools.HatenaBlog;

namespace Smdn.Applications.HatenaBlogTools {
  class HatenaDiaryFormatter : FormatterBase {
    public HatenaDiaryFormatter(
#if RETRIEVE_COMMENTS
      bool retrieveComments = false
#endif
    )
    {
    }

    public override void Format(IEnumerable<PostedEntry> entries, Stream outputStream)
    {
      var diaryElement = new XElement("diary");
      var dayElements = new Dictionary<string, XElement>();
      var defaultUpdatedDate = DateTimeOffset.FromUnixTimeSeconds(0L);

      foreach (var entry in entries) {
        var updatedDate = entry.Updated ?? defaultUpdatedDate;
        var date = updatedDate
          .ToLocalTime()
          .DateTime
          .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        XElement dayElement, bodyElement;

        if (dayElements.TryGetValue(date, out dayElement)) {
          bodyElement = dayElement.Element("body");
        }
        else {
          bodyElement = new XElement("body");

          dayElement = new XElement(
            "day",
            new XAttribute("date", date),
            new XAttribute("title", string.Empty),
            bodyElement
          );

          diaryElement.Add(dayElement);

          dayElements[date] = dayElement;
        }

        var body = new StringBuilder();

        body.AppendFormat("*{0}*", updatedDate.ToUnixTimeSeconds());

        var joinedCategory = string.Join("][", entry.Categories);

        if (0 < joinedCategory.Length)
          body.AppendFormat("[{0}]", joinedCategory);

        body.AppendLine(entry.Title);

        body.AppendLine(entry.Content);
        body.AppendLine();

        bodyElement.Add(new XText(body.ToString()));

#if RETRIEVE_COMMENTS
        if (retrieveComments) {
          var entryUrl = entry.GetSingleNodeValueOf("atom:link[@rel='alternate' and @type='text/html']/@href", nsmgr);
          var commentsElement = dayElement.AppendElement("comments");

          foreach (var comment in RetrieveComments(entryUrl)) {
            var commentElement = commentsElement.AppendElement("comment");

            commentElement.AppendElement("username").AppendText(comment.Author);
            commentElement.AppendElement("body").AppendText(comment.Content);
            commentElement.AppendElement("timestamp").AppendText(XmlConvert.ToString(comment.Date.ToUnixTimeSeconds()));
          }
        }
#endif
      }

      var outputDocument = new XDocument(new XDeclaration("1.0", "utf-8", null),
                                         diaryElement);

      outputDocument.Save(outputStream);
    }
  }
}
