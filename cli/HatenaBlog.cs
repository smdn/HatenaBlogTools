//
// Author:
//       smdn <smdn@smdn.jp>
//
// Copyright (c) 2013 smdn
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Net;
using System.Xml;

using Smdn;
using Smdn.Xml;

namespace Smdn.Applications.HatenaBlogTools {
  public class Entry {
    public string Title;
    public HashSet<string> Categories = new HashSet<string>(StringComparer.Ordinal);
    public DateTimeOffset? Updated;
    public bool IsDraft;
    public string Content;
  }

  public class PostedEntry : Entry {
    public Uri MemberUri;
    public string Author;
    public DateTimeOffset Published;
  }

  public static class HatenaBlog {
    public static void WaitForCinnamon()
    {
      System.Threading.Thread.Sleep(250);
    }

    public static List<PostedEntry> GetEntries(string hatenaId, string blogId, string apiKey)
    {
      return new List<PostedEntry>(EnumerateEntries(hatenaId, blogId, apiKey));
    }

    public static IEnumerable<PostedEntry> EnumerateEntries(string hatenaId, string blogId, string apiKey)
    {
      var atom = new Atom();

      atom.Credential = new NetworkCredential(hatenaId, apiKey);

      var rootEndPoint = new Uri(string.Concat("http://blog.hatena.ne.jp/", hatenaId, "/", blogId, "/atom/"));
      var nextUri = new Uri(rootEndPoint, "./entry");
      HttpStatusCode statusCode;

      for (;;) {
        var collectionDocument = atom.Get(nextUri, out statusCode);

        if (statusCode != HttpStatusCode.OK)
          throw new WebException(string.Format("エントリの取得に失敗したため中断しました ({0})", statusCode), WebExceptionStatus.ProtocolError);

        foreach (var entry in ReadEntries(collectionDocument)) {
          yield return entry;
        }

        // 次のatom:linkを取得する
        var nsmgr = new XmlNamespaceManager(collectionDocument.NameTable);

        nsmgr.AddNamespace("atom", Namespaces.Atom);

        nextUri = collectionDocument.GetSingleNodeValueOf("/atom:feed/atom:link[@rel='next']/@href", nsmgr, s => s == null ? null : new Uri(s));

        if (nextUri == null)
          break;
      }
    }

    private static IEnumerable<PostedEntry> ReadEntries(XmlDocument doc)
    {
      var nsmgr = new XmlNamespaceManager(doc.NameTable);

      nsmgr.PushScope();
      nsmgr.AddNamespace("atom", Namespaces.Atom);
      nsmgr.AddNamespace("app", Namespaces.App);

      foreach (XmlNode entry in doc.SelectNodes("/atom:feed/atom:entry", nsmgr)) {
        var e = new PostedEntry();

        e.MemberUri = entry.GetSingleNodeValueOf("atom:link[@rel='edit']/@href", nsmgr, s => s == null ? null : new Uri(s));
        e.Title = entry.GetSingleNodeValueOf("atom:title/text()", nsmgr);
        e.Author = entry.GetSingleNodeValueOf("atom:author/atom:name/text()", nsmgr);
        e.Content = entry.GetSingleNodeValueOf("atom:content/text()", nsmgr);
        e.Published = entry.GetSingleNodeValueOf<DateTimeOffset>("atom:published/text()", nsmgr, DateTimeOffset.Parse); // XXX
        e.Updated = entry.GetSingleNodeValueOf<DateTimeOffset>("atom:updated/text()", nsmgr, DateTimeOffset.Parse); // XXX

        foreach (XmlElement category in entry.SelectNodes("./atom:category", nsmgr)) {
          e.Categories.Add(category.GetAttribute("term"));
        }

        e.IsDraft = entry.GetSingleNodeValueOf<bool>("app:control/app:draft/text()", nsmgr, IsDraftYes);

        yield return e;
      }

      nsmgr.PopScope();
    }

    private static bool IsDraftYes(string str)
    {
      return string.Equals(str, "yes", StringComparison.OrdinalIgnoreCase);
    }

    public static XmlDocument UpdateEntry(Atom atom, PostedEntry updatingEntry, out HttpStatusCode statusCode)
    {
      statusCode = default(HttpStatusCode);

      var putDocument = CreatePostDocument(updatingEntry);

      if (updatingEntry.Author != null)
        putDocument.DocumentElement.AppendElement("author", Namespaces.Atom).AppendElement("name", Namespaces.Atom).AppendText(updatingEntry.Author);

      return atom.Put(updatingEntry.MemberUri, putDocument, out statusCode);
    }

    public static XmlDocument PostEntry(Atom atom, Uri collectionUri, Entry entry, out HttpStatusCode statusCode)
    {
      statusCode = default(HttpStatusCode);

      var postDocument = CreatePostDocument(entry);

      return atom.Post(collectionUri, postDocument, out statusCode);
    }

    private static XmlDocument CreatePostDocument(Entry entry)
    {
      var document = new XmlDocument();

      document.AppendChild(document.CreateXmlDeclaration("1.0", "utf-8", null));

      var e = document.AppendElement("entry", Namespaces.Atom);

      e.SetAttribute("xmlns", Namespaces.Atom);
      e.SetAttribute("xmlns:app", Namespaces.App);

      e.AppendElement("title", Namespaces.Atom).AppendText(entry.Title);

      if (entry.Updated.HasValue)
        e.AppendElement("updated", Namespaces.Atom).AppendText(XmlConvert.ToString(entry.Updated.Value));

      var c = e.AppendElement("content", Namespaces.Atom);

      c.SetAttribute("type", "text/plain");
      c.AppendText(entry.Content);

      foreach (var category in entry.Categories) {
        e.AppendElement("category", Namespaces.Atom).SetAttribute("term", category);
      }

      if (entry.IsDraft)
        e.AppendElement("control", Namespaces.App).AppendElement("draft", Namespaces.App).AppendText("yes");

      return document;
    }
  }
}
