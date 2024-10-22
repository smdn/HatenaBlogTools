using System;
using System.Collections.Generic;
using NUnit.Framework;

using Smdn.HatenaBlogTools.HatenaBlog;

namespace Smdn.HatenaBlogTools;

[TestFixture]
public class ReplaceContentTextTests {
  private static void EditAllEntry(
    IReadOnlyList<PostedEntry> entries,
    IHatenaBlogEntryEditor editor,
    out IReadOnlyList<PostedEntry> updatedEntries,
    out IReadOnlyList<PostedEntry> modifiedEntries
  )
  {
    var hatenaBlog = new PseudoHatenaBlogAtomPubClient(entries);

    HatenaBlogFunctions.EditAllEntry(
      hatenaBlog,
      HatenaBlogFunctions.PostMode.PostIfModified,
      editor,
      DiffGenerator.Create(silent: true, null, null, string.Empty, string.Empty),
      null,
      null,
      out updatedEntries,
      out modifiedEntries
    );
  }

  [Test]
  public void TestReplaceContent()
  {
    var entries = new [] {
      new PseudoPostedEntry() { Content = "foobar", Title = "foobar" },
    };
    var editor = new EntryEditor("foo", "bar", new EntryContentModifier());

    EditAllEntry(
      entries,
      editor,
      out _,
      out var modifiedEntries
    );

    Assert.That(modifiedEntries.Count, Is.EqualTo(1));
    Assert.That(modifiedEntries[0].Content, Is.EqualTo("barbar"));
    Assert.That(modifiedEntries[0].Title, Is.EqualTo("foobar"));
  }

  [Test]
  public void TestReplaceContent_NotModified()
  {
    var entries = new [] {
      new PseudoPostedEntry() { Content = "foobar", Title = "foobar" },
    };
    var editor = new EntryEditor("baz", "bar", new EntryContentModifier());

    EditAllEntry(
      entries,
      editor,
      out _,
      out var modifiedEntries
    );

    Assert.That(modifiedEntries.Count, Is.EqualTo(0));

    Assert.That(entries[0].Content, Is.EqualTo("foobar"));
    Assert.That(entries[0].Title, Is.EqualTo("foobar"));
  }

  [Test]
  public void TestReplaceContentRegex()
  {
    var entries = new [] {
      new PseudoPostedEntry() { Content = "foobar", Title = "foobar" },
    };
    var editor = new RegexEntryEditor(@"fo{2}", "bar", new EntryContentModifier());

    EditAllEntry(
      entries,
      editor,
      out _,
      out var modifiedEntries
    );

    Assert.That(modifiedEntries.Count, Is.EqualTo(1));
    Assert.That(modifiedEntries[0].Content, Is.EqualTo("barbar"));
    Assert.That(modifiedEntries[0].Title, Is.EqualTo("foobar"));
  }

  [Test]
  public void TestReplaceContentRegex_NotModified()
  {
    var entries = new [] {
      new PseudoPostedEntry() { Content = "foobar", Title = "foobar" },
    };
    var editor = new RegexEntryEditor(@"fo{3}", "bar", new EntryContentModifier());

    EditAllEntry(
      entries,
      editor,
      out _,
      out var modifiedEntries
    );

    Assert.That(modifiedEntries.Count, Is.EqualTo(0));

    Assert.That(entries[0].Content, Is.EqualTo("foobar"));
    Assert.That(entries[0].Title, Is.EqualTo("foobar"));
  }

  [Test]
  public void TestReplaceContentRegex_WithReplaceString()
  {
    var entries = new [] {
      new PseudoPostedEntry() { Content = "foobar", Title = "foobar" },
    };
    var editor = new RegexEntryEditor(@"(o+)", "<$1>", new EntryContentModifier());

    EditAllEntry(
      entries,
      editor,
      out _,
      out var modifiedEntries
    );

    Assert.That(modifiedEntries.Count, Is.EqualTo(1));
    Assert.That(modifiedEntries[0].Content, Is.EqualTo("f<oo>bar"));
    Assert.That(modifiedEntries[0].Title, Is.EqualTo("foobar"));
  }




  [Test]
  public void TestReplaceTitle()
  {
    var entries = new [] {
      new PseudoPostedEntry() { Content = "foobar", Title = "foobar" },
    };
    var editor = new EntryEditor("foo", "bar", new EntryTitleModifier());

    EditAllEntry(
      entries,
      editor,
      out _,
      out var modifiedEntries
    );

    Assert.That(modifiedEntries.Count, Is.EqualTo(1));
    Assert.That(modifiedEntries[0].Content, Is.EqualTo("foobar"));
    Assert.That(modifiedEntries[0].Title, Is.EqualTo("barbar"));
  }

  [Test]
  public void TestReplaceTitle_NotModified()
  {
    var entries = new [] {
      new PseudoPostedEntry() { Content = "foobar", Title = "foobar" },
    };
    var editor = new EntryEditor("baz", "bar", new EntryTitleModifier());

    EditAllEntry(
      entries,
      editor,
      out _,
      out var modifiedEntries
    );

    Assert.That(modifiedEntries.Count, Is.EqualTo(0));

    Assert.That(entries[0].Content, Is.EqualTo("foobar"));
    Assert.That(entries[0].Title, Is.EqualTo("foobar"));
  }

  [Test]
  public void TestReplaceTitleRegex()
  {
    var entries = new [] {
      new PseudoPostedEntry() { Content = "foobar", Title = "foobar" },
    };
    var editor = new RegexEntryEditor(@"fo{2}", "bar", new EntryTitleModifier());

    EditAllEntry(
      entries,
      editor,
      out _,
      out var modifiedEntries
    );

    Assert.That(modifiedEntries.Count, Is.EqualTo(1));
    Assert.That(modifiedEntries[0].Content, Is.EqualTo("foobar"));
    Assert.That(modifiedEntries[0].Title, Is.EqualTo("barbar"));
  }

  [Test]
  public void TestReplaceTitleRegex_NotModified()
  {
    var entries = new [] {
      new PseudoPostedEntry() { Content = "foobar", Title = "foobar" },
    };
    var editor = new RegexEntryEditor(@"fo{3}", "bar", new EntryTitleModifier());

    EditAllEntry(
      entries,
      editor,
      out _,
      out var modifiedEntries
    );

    Assert.That(modifiedEntries.Count, Is.EqualTo(0));

    Assert.That(entries[0].Content, Is.EqualTo("foobar"));
    Assert.That(entries[0].Title, Is.EqualTo("foobar"));
  }
}
