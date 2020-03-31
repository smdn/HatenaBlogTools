using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;

using Smdn.IO;
using Smdn.Applications.HatenaBlogTools.HatenaBlog;

namespace Smdn.Applications.HatenaBlogTools {
  [TestFixture]
  public class MovableTypeFormatterTests {
    [Test]
    public void TestToDateString()
    {
      //MM/dd/yyyy hh\:mm\:ss tt
      Assert.AreEqual(
        "03/31/2020 12:01:02 AM",
        MovableTypeFormatter.ToDateString(new DateTime(2020, 3, 31, 0, 1, 2))
      );
      Assert.AreEqual(
        "03/31/2020 01:01:02 AM",
        MovableTypeFormatter.ToDateString(new DateTime(2020, 3, 31, 1, 1, 2))
      );
      Assert.AreEqual(
        "03/31/2020 11:01:02 AM",
        MovableTypeFormatter.ToDateString(new DateTime(2020, 3, 31, 11, 1, 2))
      );
      Assert.AreEqual(
        "03/31/2020 12:01:02 PM",
        MovableTypeFormatter.ToDateString(new DateTime(2020, 3, 31, 12, 1, 2))
      );
      Assert.AreEqual(
        "03/31/2020 01:01:02 PM",
        MovableTypeFormatter.ToDateString(new DateTime(2020, 3, 31, 13, 1, 2))
      );
      Assert.AreEqual(
        "03/31/2020 11:01:02 PM",
        MovableTypeFormatter.ToDateString(new DateTime(2020, 3, 31, 23, 1, 2))
      );
    }

    private static void Format(
      IEnumerable<PostedEntry> entries,
      out string formattedText,
      out IReadOnlyList<string> formattedLines
    )
    {
      using (var stream = new MovableTypeFormatter().ToStream(entries)) {
        formattedLines = new StreamReader(stream, Encoding.UTF8).ReadAllLines();
        formattedText = string.Join("\n", formattedLines);
      }
    }

    [Test]
    public void TestFormat_SingleEntry()
    {
      var entry = new PostedEntry() {
        Title = "entry0",
        Id = new Uri("tag:blog.example.com,2020:entry0"),
        Categories = new HashSet<string>(StringComparer.Ordinal) {"entry0-category0", "entry0-category1", "entry0-category2"},
        Updated = new DateTimeOffset(2020, 3, 31, 0, 0, 0, TimeSpan.FromHours(+9)),
        IsDraft = false,
        Summary = "entry0-summary",
        Content = "entry0-content",
        ContentType = "text/x-hatena-syntax",
        MemberUri = new Uri("https://blog.example.com/atom/entry/0/"),
        EntryUri = new Uri("https://example.com/entry/0/"),
        Author = "entry0-author",
        Published = new DateTimeOffset(2020, 3, 31, 0, 0, 0, TimeSpan.FromHours(0)),
        FormattedContent = "entry0-formatted-content"
      };

      const string expectedResult = @"AUTHOR: entry0-author
TITLE: entry0
BASENAME: 0/
STATUS: Publish
CONVERT BREAKS: 0
DATE: 03/31/2020 12:00:00 AM
TAGS: entry0-category0,entry0-category1,entry0-category2
-----
BODY:
entry0-formatted-content
-----
--------";

      Format(new[] { entry }, out var formattedText, out var _);

      Assert.AreEqual(
        expectedResult.Replace("\r", string.Empty),
        formattedText
      );
    }

    [Test]
    public void TestFormat_MultipleEntries()
    {
      var entries = new[] {
        new PostedEntry() {
          Title = "entry0",
          Content = "entry0-content",
          FormattedContent = "entry0-formatted-content",
          IsDraft = false,
        },
        new PostedEntry() {
          Title = "entry1",
          Content = "entry1-content",
          FormattedContent = "entry1-formatted-content",
          IsDraft = true,
        },
      };

      const string expectedResult = @"AUTHOR: 
TITLE: entry0
STATUS: Publish
CONVERT BREAKS: 0
TAGS: 
-----
BODY:
entry0-formatted-content
-----
--------
AUTHOR: 
TITLE: entry1
STATUS: Draft
CONVERT BREAKS: 0
TAGS: 
-----
BODY:
entry1-formatted-content
-----
--------";

      Format(entries, out var formattedText, out var _);

      Assert.AreEqual(
        expectedResult.Replace("\r", string.Empty),
        formattedText
      );
    }
  }
}
