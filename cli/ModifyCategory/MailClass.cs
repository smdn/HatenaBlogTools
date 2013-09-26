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

using Smdn.Xml;

namespace Smdn.Applications.HatenaBlogTools {
  class MainClass {
    class Entry {
      public bool Modified = false;
      public Uri MemberUri;
      public string Title;
      public string Author;
      public string Content;
      public string Published;
      public HashSet<string> Categories = new HashSet<string>();
      public string Draft;
    }

    public static void Main(string[] args)
    {
      string hatenaId = null;
      string blogId = null;
      string apiKey = null;
      bool dryrun = false;
      var categoryMap = new Dictionary<string, string>();

      for (var i = 0; i < args.Length; i++) {
        switch (args[i]) {
          case "-id":
            hatenaId = args[++i];
            break;

          case "-blogid":
            blogId = args[++i];
            break;

          case "-apikey":
            apiKey = args[++i];
            break;

          case "-n":
            dryrun = true;
            break;

          case "/help":
          case "-h":
          case "--help":
            Usage(null);
            break;

          default: {
            var pos = args[i].IndexOf(':');

            if (1 <= pos) // 1文字以上
              categoryMap[args[i].Substring(0, pos)] = args[i].Substring(pos + 1);

            break;
          }
        }
      }

      if (string.IsNullOrEmpty(hatenaId))
        Usage("hatena-idを指定してください");

      if (string.IsNullOrEmpty(blogId))
        Usage("blog-idを指定してください");

      if (string.IsNullOrEmpty(apiKey))
        Usage("api-keyを指定してください");

      if (categoryMap.Count <= 0)
        Usage("変更するカテゴリを指定してください");

      Console.WriteLine("以下のカテゴリを変更します");

      foreach (var pair in categoryMap) {
        if (string.IsNullOrEmpty(pair.Value))
          Console.WriteLine("[{0}] -> (削除)", pair.Key);
        else
          Console.WriteLine("[{0}] -> [{1}]", pair.Key, pair.Value);
      }

      Console.WriteLine();
      Console.WriteLine("エントリを取得中 ...");

      var entries = GetEntries(hatenaId, blogId, apiKey);

      if (entries == null)
        return;

      Console.WriteLine("以下のエントリのカテゴリが変更されます");

      foreach (var entry in entries) {
        var currentJoinedCategories = string.Join("][", entry.Categories);

        foreach (var pair in categoryMap) {
          if (entry.Categories.Contains(pair.Key)) {
            entry.Modified = true;

            entry.Categories.Remove(pair.Key);

            if (!string.IsNullOrEmpty(pair.Value))
              entry.Categories.Add(pair.Value); // replace
          }
        }

        if (!entry.Modified)
          continue;

        Console.WriteLine("{0} \"{1}\" [{2}] -> [{3}]",
                          entry.Published,
                          entry.Title,
                          currentJoinedCategories,
                          string.Join("][", entry.Categories));
      }

      if (dryrun)
        return;

      if (!ConsoleUtils.AskYesNo("変更しますか ")) {
        Console.WriteLine("変更を中断しました");
        return;
      }

      Console.WriteLine();

      foreach (var entry in entries) {
        if (!entry.Modified)
          continue;

        Console.Write("変更を更新中: {0} \"{1}\" [{2}] ... ",
                      entry.Published,
                      entry.Title,
                      string.Join("][", entry.Categories));

        var status = PostEntry(hatenaId, apiKey, entry);

        if (status == HttpStatusCode.OK)
          Console.WriteLine();
        else {
          Console.WriteLine("失敗しました ({0})", status);
          return;
        }

        System.Threading.Thread.Sleep(250);
      }

      Console.WriteLine("変更が完了しました");
    }

    private static List<Entry> GetEntries(string hatenaId, string blogId, string apiKey)
    {
      var atom = new Atom();

      atom.Credential = new NetworkCredential(hatenaId, apiKey);

      var rootEndPoint = new Uri(string.Concat("http://blog.hatena.ne.jp/", hatenaId, "/", blogId, "/atom/"));
      var nextUri = new Uri(rootEndPoint, "./entry");
      HttpStatusCode statusCode;

      var entries = new List<Entry>();

      for (;;) {
        var collectionDocument = atom.Get(nextUri, out statusCode);

        if (statusCode != HttpStatusCode.OK) {
          Console.Error.WriteLine("中断しました ({0})", statusCode);
          return null;
        }

        entries.AddRange(ReadEntries(collectionDocument));

        // 次のatom:linkを取得する
        var nsmgr = new XmlNamespaceManager(collectionDocument.NameTable);

        nsmgr.AddNamespace("atom", Namespaces.Atom);

        nextUri = collectionDocument.GetSingleNodeValueOf("/atom:feed/atom:link[@rel='next']/@href", nsmgr, s => s == null ? null : new Uri(s));

        if (nextUri == null)
          break;
      }

      return entries;
    }

    private static IEnumerable<Entry> ReadEntries(XmlDocument doc)
    {
      var nsmgr = new XmlNamespaceManager(doc.NameTable);

      nsmgr.PushScope();
      nsmgr.AddNamespace("atom", Namespaces.Atom);
      nsmgr.AddNamespace("app", Namespaces.App);

      foreach (XmlNode entry in doc.SelectNodes("/atom:feed/atom:entry", nsmgr)) {
        var e = new Entry();

        e.MemberUri = entry.GetSingleNodeValueOf("atom:link[@rel='edit']/@href", nsmgr, s => s == null ? null : new Uri(s));
        e.Title = entry.GetSingleNodeValueOf("atom:title/text()", nsmgr);
        e.Author = entry.GetSingleNodeValueOf("atom:author/atom:name/text()", nsmgr);
        e.Content = entry.GetSingleNodeValueOf("atom:content/text()", nsmgr);
        e.Published = entry.GetSingleNodeValueOf("atom:published/text()", nsmgr);

        foreach (XmlElement category in entry.SelectNodes("./atom:category", nsmgr)) {
          e.Categories.Add(category.GetAttribute("term"));
        }

        e.Draft = entry.GetSingleNodeValueOf("app:control/app:draft/text()", nsmgr);

        yield return e;
      }

      nsmgr.PopScope();
    }

    private static HttpStatusCode PostEntry(string hatenaId, string apiKey, Entry entry)
    {
      var doc = new XmlDocument();

      doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));

      var e = doc.AppendElement("entry", Namespaces.Atom);

      e.SetAttribute("xmlns", Namespaces.Atom);
      e.SetAttribute("xmlns:app", Namespaces.App);

      e.AppendElement("title", Namespaces.Atom).AppendText(entry.Title);
      e.AppendElement("author", Namespaces.Atom).AppendElement("name", Namespaces.Atom).AppendText(entry.Author);

      var c = e.AppendElement("content", Namespaces.Atom);

      c.SetAttribute("type", "text/plain");
      c.AppendText(entry.Content);

      foreach (var category in entry.Categories) {
        e.AppendElement("category", Namespaces.Atom).SetAttribute("term", category);
      }

      e.AppendElement("control", Namespaces.App).AppendElement("draft", Namespaces.App).AppendText(entry.Draft);

      var atom = new Atom();

      atom.Credential = new NetworkCredential(hatenaId, apiKey);

      HttpStatusCode statusCode;

      atom.Put(entry.MemberUri, doc, out statusCode);

      return statusCode;
    }

    private static void Usage(string format, params string[] args)
    {
      if (format != null) {
        Console.Error.Write("error: ");
        Console.Error.WriteLine(format, args);
      }

      Console.Error.WriteLine("usage:");
      Console.Error.WriteLine("  {0} -id <hatena-id> -blogid <blog-id> -apikey <api-key> new1:old1 new2:old2 ...",
                              System.IO.Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location));
      Console.Error.WriteLine("options:");
      Console.Error.WriteLine("  -n : dry run");

      Environment.Exit(-1);
    }
  }
}
