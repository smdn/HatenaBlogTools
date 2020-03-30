using System;
using System.Collections.Generic;
using NUnit.Framework;

using Smdn.Applications.HatenaBlogTools.HatenaBlog;

namespace Smdn.Applications.HatenaBlogTools {
  [TestFixture]
  public class ReplaceContentTextTests {
#if DEBUG
    private static void EditAllEntry(
      IReadOnlyList<PostedEntry> entries,
      IHatenaBlogEntryEditor editor,
      out IReadOnlyList<PostedEntry> updatedEntries,
      out IReadOnlyList<PostedEntry> modifiedEntries
    )
    {
      var hatenaBlog = HatenaBlogAtomPubClient.Create(entries);

      HatenaBlogFunctions.EditAllEntry(
        hatenaBlog,
        HatenaBlogFunctions.PostMode.PostIfModified,
        editor,
        DiffGenerator.Create(silent: true, null, null, null, null),
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
        new PostedEntry() { Content = "foobar", Title = "foobar" },
      };
      var editor = new EntryEditor("foo", "bar", new EntryContentModifier());

      EditAllEntry(
        entries,
        editor,
        out _,
        out var modifiedEntries
      );

      Assert.AreEqual(1, modifiedEntries.Count);
      Assert.AreEqual("barbar", modifiedEntries[0].Content);
      Assert.AreEqual("foobar", modifiedEntries[0].Title);
    }

    [Test]
    public void TestReplaceContent_NotModified()
    {
      var entries = new [] {
        new PostedEntry() { Content = "foobar", Title = "foobar" },
      };
      var editor = new EntryEditor("baz", "bar", new EntryContentModifier());

      EditAllEntry(
        entries,
        editor,
        out _,
        out var modifiedEntries
      );

      Assert.AreEqual(0, modifiedEntries.Count);

      Assert.AreEqual("foobar", entries[0].Content);
      Assert.AreEqual("foobar", entries[0].Title);
    }

    [Test]
    public void TestReplaceContentRegex()
    {
      var entries = new [] {
        new PostedEntry() { Content = "foobar", Title = "foobar" },
      };
      var editor = new RegexEntryEditor(@"fo{2}", "bar", new EntryContentModifier());

      EditAllEntry(
        entries,
        editor,
        out _,
        out var modifiedEntries
      );

      Assert.AreEqual(1, modifiedEntries.Count);
      Assert.AreEqual("barbar", modifiedEntries[0].Content);
      Assert.AreEqual("foobar", modifiedEntries[0].Title);
    }

    [Test]
    public void TestReplaceContentRegex_NotModified()
    {
      var entries = new [] {
        new PostedEntry() { Content = "foobar", Title = "foobar" },
      };
      var editor = new RegexEntryEditor(@"fo{3}", "bar", new EntryContentModifier());

      EditAllEntry(
        entries,
        editor,
        out _,
        out var modifiedEntries
      );

      Assert.AreEqual(0, modifiedEntries.Count);

      Assert.AreEqual("foobar", entries[0].Content);
      Assert.AreEqual("foobar", entries[0].Title);
    }

    [Test]
    public void TestReplaceContentRegex_WithReplaceString()
    {
      var entries = new [] {
        new PostedEntry() { Content = "foobar", Title = "foobar" },
      };
      var editor = new RegexEntryEditor(@"(o+)", "<$1>", new EntryContentModifier());

      EditAllEntry(
        entries,
        editor,
        out _,
        out var modifiedEntries
      );

      Assert.AreEqual(1, modifiedEntries.Count);
      Assert.AreEqual("f<oo>bar", modifiedEntries[0].Content);
      Assert.AreEqual("foobar", modifiedEntries[0].Title);
    }




    [Test]
    public void TestReplaceTitle()
    {
      var entries = new [] {
        new PostedEntry() { Content = "foobar", Title = "foobar" },
      };
      var editor = new EntryEditor("foo", "bar", new EntryTitleModifier());

      EditAllEntry(
        entries,
        editor,
        out _,
        out var modifiedEntries
      );

      Assert.AreEqual(1, modifiedEntries.Count);
      Assert.AreEqual("foobar", modifiedEntries[0].Content);
      Assert.AreEqual("barbar", modifiedEntries[0].Title);
    }

    [Test]
    public void TestReplaceTitle_NotModified()
    {
      var entries = new [] {
        new PostedEntry() { Content = "foobar", Title = "foobar" },
      };
      var editor = new EntryEditor("baz", "bar", new EntryTitleModifier());

      EditAllEntry(
        entries,
        editor,
        out _,
        out var modifiedEntries
      );

      Assert.AreEqual(0, modifiedEntries.Count);

      Assert.AreEqual("foobar", entries[0].Content);
      Assert.AreEqual("foobar", entries[0].Title);
    }

    [Test]
    public void TestReplaceTitleRegex()
    {
      var entries = new [] {
        new PostedEntry() { Content = "foobar", Title = "foobar" },
      };
      var editor = new RegexEntryEditor(@"fo{2}", "bar", new EntryTitleModifier());

      EditAllEntry(
        entries,
        editor,
        out _,
        out var modifiedEntries
      );

      Assert.AreEqual(1, modifiedEntries.Count);
      Assert.AreEqual("foobar", modifiedEntries[0].Content);
      Assert.AreEqual("barbar", modifiedEntries[0].Title);
    }

    [Test]
    public void TestReplaceTitleRegex_NotModified()
    {
      var entries = new [] {
        new PostedEntry() { Content = "foobar", Title = "foobar" },
      };
      var editor = new RegexEntryEditor(@"fo{3}", "bar", new EntryTitleModifier());

      EditAllEntry(
        entries,
        editor,
        out _,
        out var modifiedEntries
      );

      Assert.AreEqual(0, modifiedEntries.Count);

      Assert.AreEqual("foobar", entries[0].Content);
      Assert.AreEqual("foobar", entries[0].Title);
    }
#else // if DEBUG
    [Test]
    public void Test()
    {
      Assert.Warn("some test cases are available only with configuration of DEBUG");
    }
#endif
  }
}
