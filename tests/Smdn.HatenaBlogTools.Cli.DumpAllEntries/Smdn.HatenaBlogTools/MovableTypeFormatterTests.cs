using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;

using Smdn.IO;
using Smdn.HatenaBlogTools.HatenaBlog;

namespace Smdn.HatenaBlogTools {
  [TestFixture]
  public class MovableTypeFormatterTests {
    private static System.Collections.IEnumerable YieldTestCases_TestToDateString_DateTime()
    {
      //MM/dd/yyyy hh\:mm\:ss tt
      yield return new object[] { new DateTime(2020, 3, 31, 0, 1, 2), "03/31/2020 12:01:02 AM" };
      yield return new object[] { new DateTime(2020, 3, 31, 1, 1, 2), "03/31/2020 01:01:02 AM" };
      yield return new object[] { new DateTime(2020, 3, 31, 11, 1, 2), "03/31/2020 11:01:02 AM" };
      yield return new object[] { new DateTime(2020, 3, 31, 12, 1, 2), "03/31/2020 12:01:02 PM" };
      yield return new object[] { new DateTime(2020, 3, 31, 13, 1, 2), "03/31/2020 01:01:02 PM" };
      yield return new object[] { new DateTime(2020, 3, 31, 23, 1, 2), "03/31/2020 11:01:02 PM" };
    }

    [TestCaseSource(nameof(YieldTestCases_TestToDateString_DateTime))]
    public void TestToDateString_DateTime(DateTime dateTime, string expected)
      => Assert.AreEqual(expected, MovableTypeFormatter.ToDateString(dateTime));

    private static System.Collections.IEnumerable YieldTestCases_TestToDateString_DateTimeOffset()
    {
      //MM/dd/yyyy hh\:mm\:ss tt
      yield return new object[] { new DateTimeOffset(2020, 3, 31, 0, 0, 0, TimeSpan.FromHours(+9)), "03/31/2020 12:00:00 AM" };
      yield return new object[] { new DateTimeOffset(2020, 3, 31, 0, 0, 0, TimeSpan.FromHours(0)), "03/31/2020 12:00:00 AM" };
      yield return new object[] { new DateTimeOffset(2020, 3, 31, 0, 0, 0, TimeSpan.FromHours(-5)), "03/31/2020 12:00:00 AM" };
      yield return new object[] { new DateTimeOffset(2020, 3, 31, 1, 0, 0, TimeSpan.FromHours(0)), "03/31/2020 01:00:00 AM" };
      yield return new object[] { new DateTimeOffset(2020, 3, 31, 11, 0, 0, TimeSpan.FromHours(0)), "03/31/2020 11:00:00 AM" };
      yield return new object[] { new DateTimeOffset(2020, 3, 31, 12, 0, 0, TimeSpan.FromHours(+9)), "03/31/2020 12:00:00 PM" };
      yield return new object[] { new DateTimeOffset(2020, 3, 31, 12, 0, 0, TimeSpan.FromHours(0)), "03/31/2020 12:00:00 PM" };
      yield return new object[] { new DateTimeOffset(2020, 3, 31, 12, 0, 0, TimeSpan.FromHours(-5)), "03/31/2020 12:00:00 PM" };
      yield return new object[] { new DateTimeOffset(2020, 3, 31, 13, 0, 0, TimeSpan.FromHours(0)), "03/31/2020 01:00:00 PM" };
      yield return new object[] { new DateTimeOffset(2020, 3, 31, 23, 0, 0, TimeSpan.FromHours(0)), "03/31/2020 11:00:00 PM" };
    }

    [TestCaseSource(nameof(YieldTestCases_TestToDateString_DateTimeOffset))]
    public void TestToDateString_DateTimeOffset(DateTimeOffset dateTimeOffset, string expected)
      => Assert.AreEqual(expected, MovableTypeFormatter.ToDateString(dateTimeOffset));

    private static void Format(
      IEnumerable<PostedEntry> entries,
      TimeZoneInfo? entryTimeZone,
      out string formattedText,
      out IReadOnlyList<string> formattedLines
    )
    {
      var formatter = new MovableTypeFormatter();

      if (entryTimeZone is not null)
        formatter.EntryTimeZone = entryTimeZone;

      using (var stream = formatter.ToStream(entries)) {
        formattedLines = new StreamReader(stream, Encoding.UTF8).ReadAllLines();
        formattedText = string.Join("\n", formattedLines);
      }
    }

    private static System.Collections.IEnumerable YieldTestCases_TestFormat_EntryTimeZone()
    {
      var dateTime_OffsetPlus0900 = new DateTimeOffset(2020, 3, 31, 0, 0, 0, TimeSpan.FromHours(+9));

      yield return new object[] {
        dateTime_OffsetPlus0900,
        Runtime.IsRunningOnNetFx ? "Tokyo Standard Time" : "Asia/Tokyo",
        "03/31/2020 12:00:00 AM"
      };
      yield return new object[] {
        dateTime_OffsetPlus0900,
        Runtime.IsRunningOnNetFx ? "Eastern Standard Time" : "America/New_York",
        "03/30/2020 11:00:00 AM"
      };
      yield return new object[] {
        dateTime_OffsetPlus0900,
        Runtime.IsRunningOnNetFx ? "GMT Standard Time" : "Europe/London",
        "03/30/2020 04:00:00 PM"
      };

      var dateTime_OffsetMinus0500DST = new DateTimeOffset(2020, 3, 31, 21, 0, 0, TimeSpan.FromHours(-5));

      yield return new object[] {
        dateTime_OffsetMinus0500DST,
        Runtime.IsRunningOnNetFx ? "Tokyo Standard Time" : "Asia/Tokyo",
        "04/01/2020 11:00:00 AM"
      };
      yield return new object[] {
        dateTime_OffsetMinus0500DST,
        Runtime.IsRunningOnNetFx ? "Eastern Standard Time" : "America/New_York",
        "03/31/2020 10:00:00 PM"
      };
      yield return new object[] {
        dateTime_OffsetMinus0500DST,
        Runtime.IsRunningOnNetFx ? "GMT Standard Time" : "Europe/London",
        "04/01/2020 03:00:00 AM"
      };

      var dateTime_OffsetMinus0500 = new DateTimeOffset(2020, 12, 1, 21, 0, 0, TimeSpan.FromHours(-5));

      yield return new object[] {
        dateTime_OffsetMinus0500,
        Runtime.IsRunningOnNetFx ? "Tokyo Standard Time" : "Asia/Tokyo",
        "12/02/2020 11:00:00 AM"
      };
      yield return new object[] {
        dateTime_OffsetMinus0500,
        Runtime.IsRunningOnNetFx ? "Eastern Standard Time" : "America/New_York",
        "12/01/2020 09:00:00 PM"
      };
      yield return new object[] {
        dateTime_OffsetMinus0500,
        Runtime.IsRunningOnNetFx ? "GMT Standard Time" : "Europe/London",
        "12/02/2020 02:00:00 AM"
      };
    }

    [TestCaseSource(nameof(YieldTestCases_TestFormat_EntryTimeZone))]
    public void TestFormat_EntryTimeZone(DateTimeOffset dateUpdated, string timeZoneId, string expectedDateString)
    {
      var entry = new PseudoPostedEntry(
        id: new Uri("tag:blog.example.com,2020:entry0"),
        memberUri: new Uri("https://blog.example.com/atom/entry/0/"),
        entryUri: new Uri("https://example.com/entry/0/"),
        datePublished: DateTimeOffset.Now // this value will not be used
      ) {
        Title = "entry0",
        DateUpdated = dateUpdated,
      };

      Format(new[] { entry }, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId), out var formattedText, out var _);

      StringAssert.Contains(
        "DATE: " + expectedDateString,
        formattedText
      );
    }

    private static System.Collections.IEnumerable YieldTestCases_TestFormat_SingleEntry()
    {
      yield return new object[] { new DateTimeOffset(2020, 3, 31, 0, 0, 0, TimeSpan.FromHours(+9)) };
      yield return new object[] { new DateTimeOffset(2020, 3, 31, 21, 0, 0, TimeSpan.FromHours(-5)) }; // DST
      yield return new object[] { new DateTimeOffset(2020, 12, 1, 21, 0, 0, TimeSpan.FromHours(-5)) };

    }

    [TestCaseSource(nameof(YieldTestCases_TestFormat_SingleEntry))]
    public void TestFormat_SingleEntry(DateTimeOffset dateUpdated)
    {
      var entry = new PseudoPostedEntry(
        id: new Uri("tag:blog.example.com,2020:entry0"),
        memberUri: new Uri("https://blog.example.com/atom/entry/0/"),
        entryUri: new Uri("https://example.com/entry/0/"),
        datePublished: DateTimeOffset.Now, // this value will not be used
        authors: new[] {"entry0-author0", "entry0-author1"},
        categories: new[] {"entry0-category0", "entry0-category1", "entry0-category2"},
        formattedContent: "entry0-formatted-content"
      ) {
        Title = "entry0",
        DateUpdated = dateUpdated,
        IsDraft = false,
        Summary = "entry0-summary",
        Content = "entry0-content",
        ContentType = "text/x-hatena-syntax",
      };

      var expectedResult = @$"AUTHOR: entry0-author0 entry0-author1
TITLE: entry0
BASENAME: 0/
STATUS: Publish
CONVERT BREAKS: 0
DATE: {MovableTypeFormatter.ToDateString(dateUpdated.LocalDateTime)}
TAGS: entry0-category0,entry0-category1,entry0-category2
-----
BODY:
entry0-formatted-content
-----
--------";

      Format(new[] { entry }, null, out var formattedText, out var _);

      Assert.AreEqual(
        expectedResult.Replace("\r", string.Empty),
        formattedText
      );
    }

    [Test]
    public void TestFormat_MultipleEntries()
    {
      var entries = new[] {
        new PseudoPostedEntry(
          formattedContent: "entry0-formatted-content"
        ) {
          Title = "entry0",
          Content = "entry0-content",
          IsDraft = false,
        },
        new PseudoPostedEntry(
          formattedContent: "entry1-formatted-content"
        ) {
          Title = "entry1",
          Content = "entry1-content",
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

      Format(entries, null, out var formattedText, out var _);

      Assert.AreEqual(
        expectedResult.Replace("\r", string.Empty),
        formattedText.Replace(" \n", "\n") // trim line endings
      );
    }
  }
}
