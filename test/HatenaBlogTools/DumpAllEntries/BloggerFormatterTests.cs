using System;
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
      var entry = new PseudoPostedEntry(
        id: new Uri("tag:blog.example.com,2020:entry0"),
        memberUri: new Uri("https://blog.example.com/atom/entry/0/"),
        entryUri: new Uri("https://example.com/entry/0/"),
        datePublished: new DateTimeOffset(2020, 3, 31, 0, 0, 0, TimeSpan.FromHours(0)),
        authors: new[] {"entry0-author0", "entry0-author1"},
        categories: new[] {"entry0-category0", "entry0-category1", "entry0-category2"},
        formattedContent: "entry0-formatted-content"
      ) {
        Title = "entry0",
        DateUpdated = new DateTimeOffset(2020, 3, 31, 0, 0, 0, TimeSpan.FromHours(+9)),
        IsDraft = false,
        Summary = "entry0-summary",
        Content = "entry0-content",
        ContentType = "text/x-hatena-syntax",
      };

      var doc = XDocument.Load(new BloggerFormatter(blogTitle).ToStream(new[] { entry }));

      // /feed
      var elementFeed = doc.Root;

      Assert.AreEqual(
        AtomPub.Namespaces.Atom + "feed",
        elementFeed.Name
      );

      // /feed/title
      Assert.AreEqual(
        blogTitle,
        elementFeed.Element(AtomPub.Namespaces.Atom + "title")?.Value
      );

      // /feed/generator
      Assert.AreEqual(
        "Blogger",
        elementFeed.Element(AtomPub.Namespaces.Atom + "generator")?.Value
      );

      // /feed/entry
      Assert.AreEqual(
        1,
        elementFeed.Elements(AtomPub.Namespaces.Atom + "entry").Count()
      );

      var elementEntry = elementFeed.Element(AtomPub.Namespaces.Atom + "entry");

      // /feed/entry/id
      Assert.AreEqual(
        entry.Id.AbsoluteUri,
        elementEntry.Element(AtomPub.Namespaces.Atom + "id")?.Value
      );

      // /feed/entry/author
      CollectionAssert.AreEquivalent(
        entry.Authors,
        elementEntry.Elements(AtomPub.Namespaces.Atom + "author").Elements(AtomPub.Namespaces.Atom + "name").Select(e => e.Value)
      );

      // /feed/entry/published
      Assert.AreEqual(
        entry.DatePublished,
        DateTimeOffset.Parse(elementEntry.Element(AtomPub.Namespaces.Atom + "published")?.Value)
      );

      // /feed/entry/updated
      Assert.AreEqual(
        entry.DateUpdated,
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
        "html",
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
      var entry = new PseudoPostedEntry(
        id: new Uri("tag:blog.example.com,2020:entry0"),
        formattedContent: "entry0-formatted-content"
      ) {
        Title = "entry0",
        IsDraft = true,
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
        new PseudoPostedEntry(
          id: new Uri("tag:blog.example.com,2020:entry0"),
          formattedContent: "entry0-formatted-content"
        ) {
          Title = "entry0",
          Content = "entry0-content",
        },
        new PseudoPostedEntry(
          id: new Uri("tag:blog.example.com,2020:entry1"),
          formattedContent: "entry1-formatted-content"
        ) {
          Title = "entry1",
          Content = "entry1-content",
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
