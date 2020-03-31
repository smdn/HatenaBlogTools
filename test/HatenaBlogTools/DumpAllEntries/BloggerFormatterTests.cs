using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

using Smdn.Applications.HatenaBlogTools.HatenaBlog;
using Smdn.Applications.HatenaBlogTools.AtomPublishingProtocol;
using Smdn.Xml.Linq;

namespace Smdn.Applications.HatenaBlogTools {
  [TestFixture]
  public class BloggerFormatterTests {
    [Test]
    public void TestFormat_SingleEntry()
    {
      const string blogTitle = "my test blog";
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

      var doc = XDocument.Load(new BloggerFormatter(blogTitle).ToStream(new[] { entry }));

      // /feed
      var elementFeed = doc.Root;

      Assert.AreEqual(
        AtomPub.Namespaces.Atom + "feed",
        elementFeed.Name
      );

      // /feed/link
      Assert.AreEqual(
        "http://www.blogger.com/",
        elementFeed.Elements(AtomPub.Namespaces.Atom + "link").FirstOrDefault(e => e.HasAttributeWithValue("rel", "self"))?.GetAttributeValue("href")
      );
      Assert.AreEqual(
        "application/atom+xml",
        elementFeed.Elements(AtomPub.Namespaces.Atom + "link").FirstOrDefault(e => e.HasAttributeWithValue("rel", "self"))?.GetAttributeValue("type")
      );

      Assert.AreEqual(
        "http://www.blogger.com/",
        elementFeed.Elements(AtomPub.Namespaces.Atom + "link").FirstOrDefault(e => e.HasAttributeWithValue("rel", "alternate"))?.GetAttributeValue("href")
      );
      Assert.AreEqual(
        "text/html",
        elementFeed.Elements(AtomPub.Namespaces.Atom + "link").FirstOrDefault(e => e.HasAttributeWithValue("rel", "alternate"))?.GetAttributeValue("type")
      );

      // /feed/title
      Assert.AreEqual(
        blogTitle,
        elementFeed.Element(AtomPub.Namespaces.Atom + "title")?.Value
      );

      // /feed/generator
      Assert.IsNotEmpty(elementFeed.Element(AtomPub.Namespaces.Atom + "generator")?.Value);

      // /feed/updated
      Assert.DoesNotThrow(() => {
        DateTimeOffset.Parse(elementFeed.Element(AtomPub.Namespaces.Atom + "updated")?.Value);
      });

      // /feed/entry
      Assert.AreEqual(
        1,
        elementFeed.Elements(AtomPub.Namespaces.Atom + "entry").Count()
      );

      var elementEntry = elementFeed.Element(AtomPub.Namespaces.Atom + "entry");

      // /feed/entry/link
      Assert.AreEqual(
        "http://www.blogger.com/",
        elementEntry.Elements(AtomPub.Namespaces.Atom + "link").FirstOrDefault(e => e.HasAttributeWithValue("rel", "self"))?.GetAttributeValue("href")
      );
      Assert.AreEqual(
        "application/atom+xml",
        elementEntry.Elements(AtomPub.Namespaces.Atom + "link").FirstOrDefault(e => e.HasAttributeWithValue("rel", "self"))?.GetAttributeValue("type")
      );

      Assert.AreEqual(
        "http://www.blogger.com/",
        elementEntry.Elements(AtomPub.Namespaces.Atom + "link").FirstOrDefault(e => e.HasAttributeWithValue("rel", "alternate"))?.GetAttributeValue("href")
      );
      Assert.AreEqual(
        "text/html",
        elementEntry.Elements(AtomPub.Namespaces.Atom + "link").FirstOrDefault(e => e.HasAttributeWithValue("rel", "alternate"))?.GetAttributeValue("type")
      );

      // /feed/entry/id
      Assert.AreEqual(
        entry.Id.AbsoluteUri,
        elementEntry.Element(AtomPub.Namespaces.Atom + "id")?.Value
      );

      // /feed/entry/author
      Assert.AreEqual(
        entry.Author,
        elementEntry.Element(AtomPub.Namespaces.Atom + "author")?.Element(AtomPub.Namespaces.Atom + "name")?.Value
      );

      // /feed/entry/published
      Assert.AreEqual(
        entry.Published,
        DateTimeOffset.Parse(elementEntry.Element(AtomPub.Namespaces.Atom + "published")?.Value)
      );

      // /feed/entry/updated
      Assert.AreEqual(
        entry.Updated,
        DateTimeOffset.Parse(elementEntry.Element(AtomPub.Namespaces.Atom + "updated")?.Value)
      );

      // /feed/entry/title
      Assert.AreEqual(
        entry.Title,
        elementEntry.Element(AtomPub.Namespaces.Atom + "title")?.Value
      );

      // /feed/entry/app:control/app:draft
      Assert.IsNull(elementEntry.Element(AtomPub.Namespaces.App + "control"));

      // /feed/entry/category
      Assert.AreEqual(
        "http://schemas.google.com/blogger/2008/kind#post",
        elementEntry
          .Elements(AtomPub.Namespaces.Atom + "category")
          .FirstOrDefault(e => e.HasAttributeWithValue("scheme", "http://schemas.google.com/g/2005#kind"))
          ?.GetAttributeValue("term")
      );

      var categories = elementEntry
        .Elements(AtomPub.Namespaces.Atom + "category")
        .Where(e => e.HasAttributeWithValue("scheme", "http://www.blogger.com/atom/ns#"))
        .Select(e => e.GetAttributeValue("term"));

      CollectionAssert.AreEquivalent(
        entry.Categories,
        categories
      );

      // /feed/entry/content
      Assert.AreEqual(
        "text/html",
        elementEntry.Element(AtomPub.Namespaces.Atom + "content")?.GetAttributeValue("type")
      );

      Assert.AreEqual(
        entry.FormattedContent,
        elementEntry.Element(AtomPub.Namespaces.Atom + "content")?.Value
      );
    }

    [Test]
    public void TestFormat_DraftEntry()
    {
      const string blogTitle = "my test blog";
      var entry = new PostedEntry() {
        Title = "entry0",
        Id = new Uri("tag:blog.example.com,2020:entry0"),
        IsDraft = true,
        FormattedContent = "entry0-formatted-content"
      };

      var doc = XDocument.Load(new BloggerFormatter(blogTitle).ToStream(new[] { entry }));

      // /feed
      var elementEntry = doc.Root.Element(AtomPub.Namespaces.Atom + "entry");

      // /feed/entry/app:control/app:draft
      Assert.IsNotNull(elementEntry.Element(AtomPub.Namespaces.App + "control"));
      Assert.IsNotNull(elementEntry.Element(AtomPub.Namespaces.App + "control").Element(AtomPub.Namespaces.App + "draft"));
      Assert.AreEqual(
        "yes",
        elementEntry.Element(AtomPub.Namespaces.App + "control").Element(AtomPub.Namespaces.App + "draft").Value
      );
    }

    [Test]
    public void TestFormat_MultipleEntries()
    {
      var entries = new[] {
        new PostedEntry() {
          Title = "entry0",
          Id = new Uri("tag:blog.example.com,2020:entry0"),
          Content = "entry0-content",
          FormattedContent = "entry0-formatted-content",
        },
        new PostedEntry() {
          Title = "entry1",
          Id = new Uri("tag:blog.example.com,2020:entry1"),
          Content = "entry1-content",
          FormattedContent = "entry1-formatted-content",
        },
      };

      var doc = XDocument.Load(new BloggerFormatter().ToStream(entries));

      // /feed/entry
      var elementListEntries = doc.Root.Elements(AtomPub.Namespaces.Atom + "entry").ToList();

      Assert.AreEqual(2, elementListEntries.Count);

      for (var index = 0; index < entries.Length; index++) {
        // /feed/entry[index]/id
        Assert.AreEqual(
          entries[index].Id.AbsoluteUri,
          elementListEntries[index].Element(AtomPub.Namespaces.Atom + "id")?.Value
        );

        // /feed/entry[index]/title
        Assert.AreEqual(
          entries[index].Title,
          elementListEntries[index].Element(AtomPub.Namespaces.Atom + "title")?.Value
        );

        // /feed/entry[index]/title
        Assert.AreEqual(
          entries[index].FormattedContent,
          elementListEntries[index].Element(AtomPub.Namespaces.Atom + "content")?.Value
        );
      }
    }
  }
}
