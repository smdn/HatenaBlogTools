using System;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

using Smdn.HatenaBlogTools.HatenaBlog;
using Smdn.HatenaBlogTools.AtomPublishingProtocol;
using Smdn.Xml.Linq;

namespace Smdn.HatenaBlogTools;

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
    var elementFeed = doc.Root!;

    Assert.That(
      elementFeed.Name,
      Is.EqualTo(AtomPub.Namespaces.Atom + "feed")
    );

    // /feed/title
    Assert.That(
      elementFeed.Element(AtomPub.Namespaces.Atom + "title")?.Value,
      Is.EqualTo(blogTitle)
    );

    // /feed/generator
    Assert.That(
      elementFeed.Element(AtomPub.Namespaces.Atom + "generator")?.Value,
      Is.EqualTo("Blogger")
    );

    // /feed/entry
    var elementsEntryOfPost = elementFeed
      .Elements(AtomPub.Namespaces.Atom + "entry")
      .Where(e =>
        e.Element(AtomPub.Namespaces.Atom + "category")?.HasAttributeWithValue("term", "http://schemas.google.com/blogger/2008/kind#post") ?? false
      ).ToList();

    Assert.That(
      elementsEntryOfPost.Count,
      Is.EqualTo(1)
    );

    var elementEntry = elementsEntryOfPost.First();

    // /feed/entry/id
    Assert.That(
      elementEntry.Element(AtomPub.Namespaces.Atom + "id")?.Value,
      Is.EqualTo(entry.Id!.AbsoluteUri)
    );

    // /feed/entry/author
    Assert.That(
      elementEntry.Elements(AtomPub.Namespaces.Atom + "author").Elements(AtomPub.Namespaces.Atom + "name").Select(e => e.Value),
      Is.EquivalentTo(entry.Authors)
    );

    // /feed/entry/published
    Assert.That(elementEntry.Element(AtomPub.Namespaces.Atom + "published"), Is.Not.Null);
    Assert.That(
      DateTimeOffset.Parse(elementEntry.Element(AtomPub.Namespaces.Atom + "published")!.Value, formatProvider: null),
      Is.EqualTo(entry.DatePublished)
    );

    // /feed/entry/updated
    Assert.That(elementEntry.Element(AtomPub.Namespaces.Atom + "published"), Is.Not.Null);
    Assert.That(
      DateTimeOffset.Parse(elementEntry.Element(AtomPub.Namespaces.Atom + "updated")!.Value, formatProvider: null),
      Is.EqualTo(entry.DateUpdated)
    );

    // /feed/entry/title
    Assert.That(elementEntry.Element(AtomPub.Namespaces.Atom + "title"), Is.Not.Null);
    Assert.That(
      elementEntry.Element(AtomPub.Namespaces.Atom + "title")!.Value,
      Is.EqualTo(entry.Title)
    );

    // /feed/entry/app:control/app:draft
    Assert.That(elementEntry.Element(AtomPub.Namespaces.App + "control"), Is.Null);

    // /feed/entry/category
    Assert.That(
      elementEntry
        .Elements(AtomPub.Namespaces.Atom + "category")
        .FirstOrDefault(e => e.HasAttributeWithValue("scheme", "http://schemas.google.com/g/2005#kind"))
        ?.GetAttributeValue("term"),
      Is.EqualTo("http://schemas.google.com/blogger/2008/kind#post")
    );

    var categories = elementEntry
      .Elements(AtomPub.Namespaces.Atom + "category")
      .Where(e => e.HasAttributeWithValue("scheme", "http://www.blogger.com/atom/ns#"))
      .Select(e => e.GetAttributeValue("term"));

    Assert.That(
      categories,
      Is.EquivalentTo(entry.Categories)
    );

    // /feed/entry/content
    Assert.That(
      elementEntry.Element(AtomPub.Namespaces.Atom + "content")?.GetAttributeValue("type"),
      Is.EqualTo("html")
    );

    Assert.That(
      elementEntry.Element(AtomPub.Namespaces.Atom + "content")?.Value,
      Is.EqualTo(entry.FormattedContent)
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

    // /feed/entry
    var elementEntry = doc
      .Root!
      .Elements(AtomPub.Namespaces.Atom + "entry")
      .First(e =>
        e.Element(AtomPub.Namespaces.Atom + "category")?.HasAttributeWithValue("term", "http://schemas.google.com/blogger/2008/kind#post") ?? false
      );

    // /feed/entry/app:control/app:draft
    Assert.That(elementEntry.Element(AtomPub.Namespaces.App + "control"), Is.Not.Null);
    Assert.That(elementEntry.Element(AtomPub.Namespaces.App + "control")!.Element(AtomPub.Namespaces.App + "draft"), Is.Not.Null);
    Assert.That(
      elementEntry.Element(AtomPub.Namespaces.App + "control")!.Element(AtomPub.Namespaces.App + "draft")!.Value,
      Is.EqualTo("yes")
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
    var elementListEntries = doc
      .Root!
      .Elements(AtomPub.Namespaces.Atom + "entry")
      .Where(e =>
        e.Element(AtomPub.Namespaces.Atom + "category")?.HasAttributeWithValue("term", "http://schemas.google.com/blogger/2008/kind#post") ?? false
      )
      .ToList();

    Assert.That(elementListEntries.Count, Is.EqualTo(2));

    for (var index = 0; index < entries.Length; index++) {
      // /feed/entry[index]/id
      Assert.That(
        elementListEntries[index].Element(AtomPub.Namespaces.Atom + "id")?.Value,
        Is.EqualTo(entries[index].Id!.AbsoluteUri)
      );

      // /feed/entry[index]/title
      Assert.That(
        elementListEntries[index].Element(AtomPub.Namespaces.Atom + "title")?.Value,
        Is.EqualTo(entries[index].Title)
      );

      // /feed/entry[index]/title
      Assert.That(
        elementListEntries[index].Element(AtomPub.Namespaces.Atom + "content")?.Value,
        Is.EqualTo(entries[index].FormattedContent)
      );
    }
  }

#if false
  [Test]
  public void TestFormat_BlogSettings_Title()
  {
    const string blogTitle = "my test blog";
    const string blogId = "pseudo-blog-id";

    var doc = XDocument.Load(new BloggerFormatter(blogTitle: blogTitle, blogId: blogId).ToStream(Enumerable.Empty<PostedEntry>()));

    var entrySetting = doc
      .Root
      .Elements(AtomPub.Namespaces.Atom + "entry")
      .FirstOrDefault(e =>
        e.Element(AtomPub.Namespaces.Atom + "category")?.HasAttributeWithValue("term", "http://schemas.google.com/blogger/2008/kind#settings") ?? false
      );

    Assert.IsNotNull(entrySetting);

    Assert.AreEqual(
      $"tag:blogger.com,1999:blog-{blogId}.settings.BLOG_NAME",
      entrySetting.Element(AtomPub.Namespaces.Atom + "id")?.Value
    );

    Assert.AreEqual(
      $"https://www.blogger.com/feeds/{blogId}/settings/BLOG_NAME",
      entrySetting.Elements(AtomPub.Namespaces.Atom + "link").FirstOrDefault(e => e.HasAttributeWithValue("rel", "edit"))?.Attribute("href")?.Value
    );

    Assert.AreEqual(
      $"https://www.blogger.com/feeds/{blogId}/settings/BLOG_NAME",
      entrySetting.Elements(AtomPub.Namespaces.Atom + "link").FirstOrDefault(e => e.HasAttributeWithValue("rel", "self"))?.Attribute("href")?.Value
    );
  }

  [TestCase("/entry/2011/11/07/161845", "/161845.html")] // hatena blog standard
  [TestCase("/entry/20111107/1320650325", "/1320650325.html")] // hatena diary
  [TestCase("/entry/2011/11/07/週末は川に行きました", "/週末は川に行きました.html")] // title
  [TestCase("/entry/2011/11/07/went_to_the_river_on_the_weekend", "/went_to_the_river_on_the_weekend.html")] // title
  public void TestFormat_CustomPermalink(string entryUriLocalPath, string expectedCustomPermalinkLocalPath)
  {
    const string blogDomain = "blogger.example.com";
    var datePublished = new DateTimeOffset(2020, 04, 01, 0, 0, 0, TimeSpan.FromHours(+9));

    expectedCustomPermalinkLocalPath = $"/{datePublished.Year:D4}/{datePublished.Month:D2}" + expectedCustomPermalinkLocalPath;

    var entry = new PseudoPostedEntry(
      id: new Uri("tag:blog.example.com,2020:entry0"),
      entryUri: new Uri($"https://blog.example.com{entryUriLocalPath}"),
      datePublished: datePublished,
      formattedContent: "entry0-formatted-content"
    ) {
      Title = "entry",
    };

    var doc = XDocument.Load(new BloggerFormatter(blogDomain: blogDomain).ToStream(new[] { entry }));

    var linkRelAlternate = doc
      .Root
      .Elements(AtomPub.Namespaces.Atom + "entry")
      .First(e =>
        e.Element(AtomPub.Namespaces.Atom + "category")?.HasAttributeWithValue("term", "http://schemas.google.com/blogger/2008/kind#post") ?? false
      )
      .Elements(AtomPub.Namespaces.Atom + "link")
      .FirstOrDefault(e => e.HasAttributeWithValue("rel", "alternate"));

    Assert.AreEqual(
      "text/html",
      linkRelAlternate?.GetAttributeValue("type")
    );

    Assert.AreEqual(
      entry.Title,
      linkRelAlternate?.GetAttributeValue("title")
    );

    var hrefString = linkRelAlternate?.GetAttributeValue("href");

    Assert.IsNotNull(hrefString);

    var href = new Uri(hrefString);

    Assert.AreEqual(
      blogDomain,
      href.Host
    );

    Assert.AreEqual(
      expectedCustomPermalinkLocalPath,
      href.LocalPath
    );
  }

  [TestCase("/entry/2011/11/07/161845")] // hatena blog standard
  [TestCase("/entry/20111107/1320650325")] // hatena diary
  [TestCase("/entry/2011/11/07/週末は川に行きました")] // title
  [TestCase("/entry/2011/11/07/went_to_the_river_on_the_weekend")] // title
  public void TestFormat_CustomPermalink_BlogDomainNotProvided(string entryUriLocalPath)
  {
    var entry = new PseudoPostedEntry(
      id: new Uri("tag:blog.example.com,2020:entry0"),
      entryUri: new Uri($"https://blog.example.com{entryUriLocalPath}"),
      datePublished: new DateTimeOffset(2020, 04, 01, 0, 0, 0, TimeSpan.FromHours(+9)),
      formattedContent: "entry0-formatted-content"
    ) {
      Title = "entry",
    };

    var doc = XDocument.Load(new BloggerFormatter(blogDomain: null).ToStream(new[] { entry }));
    var linkRelAlternate = doc
      .Root
      .Elements(AtomPub.Namespaces.Atom + "entry")
      .First(e =>
        e.Element(AtomPub.Namespaces.Atom + "category")?.HasAttributeWithValue("term", "http://schemas.google.com/blogger/2008/kind#post") ?? false
      )
      .Elements(AtomPub.Namespaces.Atom + "link")
      .FirstOrDefault(e => e.HasAttributeWithValue("rel", "alternate"));

    Assert.IsNull(linkRelAlternate);
  }
#endif
}
