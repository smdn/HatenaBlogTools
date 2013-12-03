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
  public class Entry {
    public string Title;
    public HashSet<string> Categories = new HashSet<string>(StringComparer.Ordinal);
    public DateTime? Updated;
    public bool IsDraft;
    public string Content;
   }

  public static class HatenaBlog {
    public static void WaitForCinnamon()
    {
      System.Threading.Thread.Sleep(250);
    }

    public static XmlDocument PostEntry(Atom atom, Uri collectionUri, Entry entry, out HttpStatusCode statusCode)
    {
      statusCode = default(HttpStatusCode);

      var postDocument = new XmlDocument();

      postDocument.AppendChild(postDocument.CreateXmlDeclaration("1.0", "utf-8", null));

      var e = postDocument.AppendElement("entry", Namespaces.Atom);

      e.SetAttribute("xmlns", Namespaces.Atom);
      e.SetAttribute("xmlns:app", Namespaces.App);

      e.AppendElement("title", Namespaces.Atom).AppendText(entry.Title);

      if (entry.Updated.HasValue)
        e.AppendElement("updated", Namespaces.Atom).AppendText(XmlConvert.ToString(entry.Updated.Value, XmlDateTimeSerializationMode.RoundtripKind));

      var c = e.AppendElement("content", Namespaces.Atom);

      c.SetAttribute("type", "text/plain");
      c.AppendText(entry.Content);

      foreach (var category in entry.Categories) {
        e.AppendElement("category", Namespaces.Atom).SetAttribute("term", category);
      }

      if (entry.IsDraft)
        e.AppendElement("control", Namespaces.App).AppendElement("draft", Namespaces.App).AppendText("yes");

      return atom.Post(collectionUri, postDocument, out statusCode);
    }
  }
}
