// SPDX-FileCopyrightText: 2020 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Smdn.HatenaBlogTools.HatenaBlog;

namespace Smdn.HatenaBlogTools;

/*
 * ref: http://www.movabletype.jp/documentation/appendices/import-export-format.html
 */
public class MovableTypeFormatter : FormatterBase {
  public MovableTypeFormatter(
#if RETRIEVE_COMMENTS
    bool retrieveComments = false
#endif
  )
  {
  }

  public static string ToDateString(DateTime dateTime)
    => dateTime.ToString("MM/dd/yyyy hh\\:mm\\:ss tt", System.Globalization.CultureInfo.InvariantCulture);

  public static string ToDateString(DateTimeOffset dateTimeOffset)
    => dateTimeOffset.ToString("MM/dd/yyyy hh\\:mm\\:ss tt", System.Globalization.CultureInfo.InvariantCulture);

  public override void Format(IEnumerable<PostedEntry> entries, Stream outputStream)
  {
    var writer = new StreamWriter(outputStream, Encoding.UTF8);

    writer.NewLine = "\n"; // ???

    foreach (var entry in entries) {
      /*
       * metadata seciton
       */
      writer.WriteLine(string.Concat("AUTHOR: ", string.Join(" ", entry.Authors)));
      writer.WriteLine(string.Concat("TITLE: ", entry.Title));

      var entryLocation = entry.EntryUri?.LocalPath;

      if (entryLocation != null)
        writer.WriteLine(string.Concat("BASENAME: ", entryLocation.Substring(7))); // remove prefix '/entry/'

      writer.WriteLine(string.Concat("STATUS: ", entry.IsDraft ? "Draft" : "Publish"));
      writer.WriteLine("CONVERT BREAKS: 0");

      if (entry.DateUpdated.HasValue) {
        var date = TimeZoneInfo.ConvertTime(entry.DateUpdated.Value, EntryTimeZone);

        writer.WriteLine(string.Concat("DATE: ", ToDateString(date)));
      }

      var tags = entry
        .Categories
        .Select(tag => tag.Contains(" ") ? string.Concat("\"", tag, "\"") : tag);

      writer.WriteLine(string.Concat("TAGS: ", string.Join(",", tags)));

      /*
       * multiline field seciton
       */
      const string multilineFieldDelimiter = "-----";

      writer.WriteLine(multilineFieldDelimiter);

      writer.WriteLine("BODY:");
      //writer.WriteLine(entry.Content);
      writer.WriteLine(entry.FormattedContent);
      writer.WriteLine(multilineFieldDelimiter);

#if RETRIEVE_COMMENTS
      if (retrieveComments) {
        var entryUrl = entry.GetSingleNodeValueOf("atom:link[@rel='alternate' and @type='text/html']/@href", nsmgr);

        foreach (var comment in RetrieveComments(entryUrl)) {
          writer.WriteLine("COMMENT:");
          writer.WriteLine(string.Concat("AUTHOR: ", comment.Author));
          writer.WriteLine(string.Concat("DATE: ", ToMovableTypeDateString(comment.Date)));
          writer.WriteLine(string.Concat("URL: ", comment.Url));
          writer.WriteLine(comment.Content);

          writer.WriteLine(multilineFieldDelimiter);
        }
      }
#endif

      // end of entry
      const string entryDelimiter = "--------";

      writer.WriteLine(entryDelimiter);
    }

    writer.Flush();
  }
}
