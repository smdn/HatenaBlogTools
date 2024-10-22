using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

using Smdn.IO;
using Smdn.HatenaBlogTools.HatenaBlog;

namespace Smdn.HatenaBlogTools;

[TestFixture]
public class HatenaDiaryFormatterTests {
  private static System.Collections.IEnumerable YieldTestCases_TestFormat_EntryTimeZone()
  {
    var dateTime_OffsetPlus0900 = new DateTimeOffset(2020, 3, 31, 0, 0, 0, TimeSpan.FromHours(+9));

    yield return new object[] {
      dateTime_OffsetPlus0900,
      Runtime.SupportsIanaTimeZoneName ? "Asia/Tokyo" : "Tokyo Standard Time",
      "2020-03-31"
    };
    yield return new object[] {
      dateTime_OffsetPlus0900,
      Runtime.SupportsIanaTimeZoneName ? "America/New_York" : "Eastern Standard Time",
      "2020-03-30"
    };
    yield return new object[] {
      dateTime_OffsetPlus0900,
      Runtime.SupportsIanaTimeZoneName ? "Europe/London" :  "GMT Standard Time",
      "2020-03-30"
    };

    var dateTime_OffsetMinus0500 = new DateTimeOffset(2020, 3, 31, 21, 0, 0, TimeSpan.FromHours(-5));

    yield return new object[] {
      dateTime_OffsetMinus0500,
      Runtime.SupportsIanaTimeZoneName ? "Asia/Tokyo" : "Tokyo Standard Time",
      "2020-04-01"
    };
    yield return new object[] {
      dateTime_OffsetMinus0500,
      Runtime.SupportsIanaTimeZoneName ? "America/New_York" : "Eastern Standard Time",
      "2020-03-31"
    };
    yield return new object[] {
      dateTime_OffsetMinus0500,
      Runtime.SupportsIanaTimeZoneName ? "Europe/London" : "GMT Standard Time",
      "2020-04-01"
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

    var formatter = new HatenaDiaryFormatter() {
      EntryTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId),
    };
    var doc = XDocument.Load(formatter.ToStream(new[] { entry }));
    var day = doc.Root!.Element("day")!;

    Assert.That(day.Attribute("date")?.Value, Is.EqualTo(expectedDateString));

    var body = day.Element("body")!;
    var bodyLines = new StringReader(body.Value).ReadAllLines();
    var firstLine = bodyLines.FirstOrDefault();

    Assert.That(firstLine, Is.Not.Null);
    Assert.That(
      firstLine,
      Is.EqualTo($"*{dateUpdated.ToUnixTimeSeconds():D}*entry0")
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_TestFormat_SingleEntry()
  {
    yield return new object[] { new DateTimeOffset(2020, 3, 31, 0, 0, 0, TimeSpan.FromHours(+9)) };
    yield return new object[] { new DateTimeOffset(2020, 3, 31, 21, 0, 0, TimeSpan.FromHours(-5)) };
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

    var doc = XDocument.Load(new HatenaDiaryFormatter().ToStream(new[] { entry }));

    // /diary
    Assert.That(doc.Root!.Name.LocalName, Is.EqualTo("diary"));

    // /diary/day
    Assert.That(doc.Root!.Elements("day").Count(), Is.EqualTo(1));

    var day = doc.Root!.Element("day")!;

    Assert.That(day.Attribute("date")?.Value, Is.EqualTo(dateUpdated.LocalDateTime.ToString("yyyy-MM-dd", provider: null)));
    Assert.That(day.Attribute("title")?.Value, Is.Empty);

    // /diary/day/body
    Assert.That(day.Elements("body").Count(), Is.EqualTo(1));

    var body = day.Element("body")!;
    var bodyLines = new StringReader(body.Value).ReadAllLines();
    var firstLine = bodyLines.FirstOrDefault();

    Assert.That(firstLine, Is.Not.Null);
    Assert.That(
      firstLine,
      Is.EqualTo($"*{dateUpdated.ToUnixTimeSeconds():D}*[entry0-category0][entry0-category1][entry0-category2]entry0")
    );

    Assert.That(
      string.Join("\n", bodyLines.Skip(1)),
      Does.StartWith(entry.Content)
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_TestFormat_MultipleEntries_SameDate()
  {
    yield return new object[] {
      new DateTimeOffset(2020, 3, 31, 15, 0, 0, TimeSpan.FromHours(+9)),
      new DateTimeOffset(2020, 3, 31, 15, 30, 0, TimeSpan.FromHours(+9))
    };
    yield return new object[] {
      new DateTimeOffset(2020, 3, 31, 21, 0, 0, TimeSpan.FromHours(-5)),
      new DateTimeOffset(2020, 3, 31, 21, 30, 0, TimeSpan.FromHours(-5))
    };
  }

  [TestCaseSource(nameof(YieldTestCases_TestFormat_MultipleEntries_SameDate))]
  public void TestFormat_MultipleEntries_SameDate(DateTimeOffset dateUpdatedEntry0, DateTimeOffset dateUpdatedEntry1)
  {
    var entries = new List<PostedEntry> {
      new PseudoPostedEntry() {
        Title = "entry0",
        Content = "entry0-content",
        DateUpdated = dateUpdatedEntry0,
      },
      new PseudoPostedEntry() {
        Title = "entry1",
        Content = "entry1-content",
        DateUpdated = dateUpdatedEntry1,
      },
    };

    var doc = XDocument.Load(new HatenaDiaryFormatter().ToStream(entries));

    var day = doc.Root!.Elements("day").First(
      e => string.Equals(dateUpdatedEntry0.LocalDateTime.ToString("yyyy-MM-dd", provider: null), e.Attribute("date")?.Value, StringComparison.Ordinal)
    );
    var bodyText = day.Value;

    Assert.That(bodyText.Replace("\r", string.Empty), Does.Contain($"*{dateUpdatedEntry0.ToUnixTimeSeconds():D}*entry0\nentry0-content"));
    Assert.That(bodyText.Replace("\r", string.Empty), Does.Contain($"*{dateUpdatedEntry1.ToUnixTimeSeconds():D}*entry1\nentry1-content"));
  }

  private static System.Collections.IEnumerable YieldTestCases_TestFormat_MultipleEntries_DifferentDate()
  {
    yield return new object[] {
      new DateTimeOffset(2020, 3, 31, 15, 0, 0, TimeSpan.FromHours(+9)),
      new DateTimeOffset(2020, 4, 1, 15, 0, 0, TimeSpan.FromHours(+9))
    };
    yield return new object[] {
      new DateTimeOffset(2020, 3, 31, 21, 0, 0, TimeSpan.FromHours(-5)),
      new DateTimeOffset(2020, 4, 1, 21, 0, 0, TimeSpan.FromHours(-5))
    };
  }

  [TestCaseSource(nameof(YieldTestCases_TestFormat_MultipleEntries_DifferentDate))]
  public void TestFormat_MultipleEntries_DifferentDate(DateTimeOffset dateUpdatedEntry0, DateTimeOffset dateUpdatedEntry1)
  {
    var entries = new List<PostedEntry> {
      new PseudoPostedEntry() {
        Title = "entry0",
        Content = "entry0-content",
        DateUpdated = dateUpdatedEntry0,
      },
      new PseudoPostedEntry() {
        Title = "entry1",
        Content = "entry1-content",
        DateUpdated = dateUpdatedEntry1,
      },
    };

    var doc = XDocument.Load(new HatenaDiaryFormatter().ToStream(entries));

    var firstDay = doc.Root!.Elements("day").First(
      e => string.Equals(dateUpdatedEntry0.LocalDateTime.ToString("yyyy-MM-dd", provider: null), e.Attribute("date")?.Value, StringComparison.Ordinal)
    );

    Assert.That(firstDay.Element("body")!.Value.Replace("\r", string.Empty), Does.Contain($"*{dateUpdatedEntry0.ToUnixTimeSeconds():D}*entry0\nentry0-content\n"));

    var secondDay = doc.Root!.Elements("day").First(
      e => string.Equals(dateUpdatedEntry1.LocalDateTime.ToString("yyyy-MM-dd", provider: null), e.Attribute("date")?.Value, StringComparison.Ordinal)
    );

    Assert.That(secondDay.Element("body")!.Value.Replace("\r", string.Empty), Does.Contain($"*{dateUpdatedEntry1.ToUnixTimeSeconds():D}*entry1\nentry1-content\n"));
  }
}
