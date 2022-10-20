// SPDX-FileCopyrightText: 2020 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;

using Smdn.HatenaBlogTools.HatenaBlog;

namespace Smdn.HatenaBlogTools;

public class HatenaDiaryFormatter : FormatterBase {
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
      var updatedDate = entry.DateUpdated ?? defaultUpdatedDate;
      var date =
        TimeZoneInfo.ConvertTime(updatedDate, EntryTimeZone)
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
