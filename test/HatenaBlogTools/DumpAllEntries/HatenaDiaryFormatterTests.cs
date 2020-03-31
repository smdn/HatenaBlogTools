using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

using Smdn.IO;
using Smdn.Applications.HatenaBlogTools.HatenaBlog;

namespace Smdn.Applications.HatenaBlogTools {
  [TestFixture]
  public class HatenaDiaryFormatterTests {
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

      var doc = XDocument.Load(new HatenaDiaryFormatter().ToStream(new[] { entry }));

      // /diary
      Assert.AreEqual("diary", doc.Root.Name.LocalName);

      // /diary/day
      Assert.AreEqual(1, doc.Root.Elements("day").Count());

      var day = doc.Root.Element("day");

      Assert.AreEqual("2020-03-31", day.Attribute("date")?.Value);
      Assert.IsEmpty(day.Attribute("title")?.Value);

      // /diary/day/body
      Assert.AreEqual(1, day.Elements("body").Count());

      var body = day.Element("body");
      var bodyLines = new StringReader(body.Value).ReadLines().ToList();
      var firstLine = bodyLines.FirstOrDefault();

      Assert.IsNotNull(firstLine);
      Assert.AreEqual(
        "*1585580400*[entry0-category0][entry0-category1][entry0-category2]entry0",
        firstLine
      );

      StringAssert.StartsWith(
        entry.Content,
        string.Join("\n", bodyLines.Skip(1))
      );
    }

    [Test]
    public void TestFormat_MultipleEntries_SameDate()
    {
      var entries = new List<PostedEntry> {
        new PostedEntry() {
          Title = "entry0",
          Content = "entry0-content",
          Updated = new DateTimeOffset(2020, 3, 31, 15, 0, 0, TimeSpan.FromHours(+9)),
        },
        new PostedEntry() {
          Title = "entry1",
          Content = "entry1-content",
          Updated = new DateTimeOffset(2020, 3, 31, 16, 0, 0, TimeSpan.FromHours(+9)),
        },
      };

      var doc = XDocument.Load(new HatenaDiaryFormatter().ToStream(entries));

      var day = doc.Root.Elements("day").First(e => string.Equals("2020-03-31", e.Attribute("date")?.Value, StringComparison.Ordinal));
      var bodyText = day.Value;

      StringAssert.Contains("*1585634400*entry0\nentry0-content", bodyText.Replace("\r", string.Empty));
      StringAssert.Contains("*1585638000*entry1\nentry1-content", bodyText.Replace("\r", string.Empty));
    }

    [Test]
    public void TestFormat_MultipleEntries_DifferentDate()
    {
      var entries = new List<PostedEntry> {
        new PostedEntry() {
          Title = "entry0",
          Content = "entry0-content",
          Updated = new DateTimeOffset(2020, 3, 31, 15, 0, 0, TimeSpan.FromHours(+9)),
        },
        new PostedEntry() {
          Title = "entry1",
          Content = "entry1-content",
          Updated = new DateTimeOffset(2020, 4, 1, 15, 0, 0, TimeSpan.FromHours(+9)),
        },
      };

      var doc = XDocument.Load(new HatenaDiaryFormatter().ToStream(entries));

      var firstDay = doc.Root.Elements("day").First(e => string.Equals("2020-03-31", e.Attribute("date")?.Value, StringComparison.Ordinal));

      StringAssert.Contains("*1585634400*entry0\nentry0-content\n", firstDay.Element("body").Value.Replace("\r", string.Empty));

      var secondDay = doc.Root.Elements("day").First(e => string.Equals("2020-04-01", e.Attribute("date")?.Value, StringComparison.Ordinal));

      StringAssert.Contains("*1585720800*entry1\nentry1-content\n", secondDay.Element("body").Value.Replace("\r", string.Empty));
    }
  }
}
