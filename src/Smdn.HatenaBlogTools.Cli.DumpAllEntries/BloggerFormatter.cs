// SPDX-FileCopyrightText: 2020 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Smdn.HatenaBlogTools.AtomPublishingProtocol;
using Smdn.HatenaBlogTools.HatenaBlog;

namespace Smdn.HatenaBlogTools;

public class BloggerFormatter : FormatterBase {
  private readonly string blogTitle;
#if false
  private readonly string blogDomain;
  private readonly string blogId;
#endif

  public BloggerFormatter(
    string blogTitle = null,
    string blogDomain = null,
    string blogId = null
  )
  {
    this.blogTitle = blogTitle;
#if false
    this.blogDomain = blogDomain;
    this.blogId = blogId;
#endif
  }

  public override void Format(IEnumerable<PostedEntry> entries, Stream outputStream)
  {
#if false
    static string GetSegmentForCustomPermalinkLocalPath(PostedEntry entry)
      => entry.EntryUri.Segments.Last();
#endif

    var elementListPost = entries.Select(entry =>
      // ./entry
      new XElement(
        AtomPub.Namespaces.Atom + "entry",
        // ./entry/id
        entry.Id == null
          ? null
          : new XElement(
              AtomPub.Namespaces.Atom + "id",
              entry.Id
            ),
#if false
        // ./entry/link[@rel='alternate']
        string.IsNullOrEmpty(blogDomain)
          ? null
          : new XElement(
              AtomPub.Namespaces.Atom + "link",
              new XAttribute("rel", "alternate"),
              new XAttribute("type", "text/html"),
              new XAttribute("title", entry.Title),
              new XAttribute("href", $"https://{blogDomain}/{entry.DatePublished.Year:D4}/{entry.DatePublished.Month:D2}/{GetSegmentForCustomPermalinkLocalPath(entry)}.html")
            ),
#endif
        // ./entry/author
        entry.Authors.Select(author =>
          string.IsNullOrEmpty(author)
            ? null
            : new XElement(
                AtomPub.Namespaces.Atom + "author",
                new XElement(
                  AtomPub.Namespaces.Atom + "name",
                  author
                )
              )
        ),
        // ./entry/published
        new XElement(
          AtomPub.Namespaces.Atom + "published",
          entry.DatePublished
        ),
        // ./entry/updated
        entry.DateUpdated == null
          ? null
          : new XElement(
              AtomPub.Namespaces.Atom + "updated",
              entry.DateUpdated
            ),
        // ./entry/title
        new XElement(
          AtomPub.Namespaces.Atom + "title",
          new XAttribute("type", "text"),
          entry.Title
        ),
        // ./entry/control
        entry.IsDraft
          ? new XElement(
              AtomPub.Namespaces.App + "control",
              // ./entry/control/draft
              new XElement(
                AtomPub.Namespaces.App + "draft",
                "yes"
              )
            )
          : null,
        // ./entry/category
        new XElement(
          AtomPub.Namespaces.Atom + "category",
          new XAttribute("scheme", "http://schemas.google.com/g/2005#kind"),
          new XAttribute("term", "http://schemas.google.com/blogger/2008/kind#post")
        ),
        entry.Categories.Select(category =>
          new XElement(
            AtomPub.Namespaces.Atom + "category",
            new XAttribute("scheme", "http://www.blogger.com/atom/ns#"),
            new XAttribute("term", category)
          )
        ),
        // ./entry/content
        new XElement(
          AtomPub.Namespaces.Atom + "content",
          new XAttribute("type", "html"),
          entry.FormattedContent
        )
      )
    );

#if false // not works
    var elementListSettings = new[] {
      // ./entry
      new XElement(
        AtomPub.Namespaces.Atom + "entry",
        // ./entry/id
        new XElement(
          AtomPub.Namespaces.Atom + "id",
          $"tag:blogger.com,1999:blog-{blogId}.settings.BLOG_NAME"
        ),
        // ./entry/category
        new XElement(
          AtomPub.Namespaces.Atom + "category",
          new XAttribute("scheme", "http://schemas.google.com/g/2005#kind"),
          new XAttribute("term", "http://schemas.google.com/blogger/2008/kind#settings")
        ),
        // ./entry/link
        new XElement(
          AtomPub.Namespaces.Atom + "link",
          new XAttribute("rel", "edit"),
          new XAttribute("type", "application/atom+xml"),
          new XAttribute("href", $"https://www.blogger.com/feeds/{blogId}/settings/BLOG_NAME")
        ),
        new XElement(
          AtomPub.Namespaces.Atom + "link",
          new XAttribute("rel", "self"),
          new XAttribute("type", "application/atom+xml"),
          new XAttribute("href", $"https://www.blogger.com/feeds/{blogId}/settings/BLOG_NAME")
        ),
        // ./entry/content
        new XElement(
          AtomPub.Namespaces.Atom + "content",
          new XAttribute("type", "text"),
          blogTitle
        )
      )
    };
#endif

    var document = new XDocument(
      new XDeclaration("1.0", "utf-8", null),
      // /feed
      new XElement(
        AtomPub.Namespaces.Atom + "feed",
        // new XAttribute(XNamespace.Xmlns + string.Empty, AtomPub.Namespaces.Atom),
        new XAttribute(XNamespace.Xmlns + "app", AtomPub.Namespaces.App),
        // /feed/title
        string.IsNullOrEmpty(blogTitle)
          ? null
          : new XElement(
              AtomPub.Namespaces.Atom + "title",
              blogTitle
            ),
        // /feed/generator
        new XElement(
          AtomPub.Namespaces.Atom + "generator",
          "Blogger"
        ),
#if false
        // /feed/entry (http://schemas.google.com/blogger/2008/kind#settings)
        elementListSettings,
#endif
        // /feed/entry (http://schemas.google.com/blogger/2008/kind#post)
        elementListPost
      )
    );

    document.Save(outputStream);
  }
}
