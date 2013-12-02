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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using Smdn.Xml;

namespace Smdn.Applications.HatenaBlogTools {
  class MainClass {
    private class Entry {
      public HashSet<string> Categories = new HashSet<string>(StringComparer.Ordinal);
      public DateTime Updated;
      public string Title;
      public string Content;
    }

    public static void Main(string[] args)
    {
      var exportedGroupDocument = new XmlDocument();

      //exportedGroupDocument.Load("/home/smdn/group-export.xml");
      exportedGroupDocument.Load("/home/smdn/group-old-exprot.xml");
      //

      foreach (var entry in GetEntries(exportedGroupDocument)) {
        //Console.WriteLine("{2} [{1}]|{0}", entry.Title, string.Join("][", entry.Categories), entry.Updated);
        //Console.WriteLine(entry.Content);
      }
    }

    private static readonly Regex entryHeaderRegex = new Regex(@"^\*((?<timestamp>[0-9]+)|(?<name>[^\*]+))\*(\[(?<category>[^\[]+)\]\s*)*(?<title>.*)$", RegexOptions.Singleline);

    private static IEnumerable<Entry> GetEntries(XmlDocument exportedGroupDocument)
    {
      foreach (XmlElement dayElement in exportedGroupDocument.SelectNodes("/diary/day")) {
        var entry = new Entry();
        var date = DateTime.ParseExact(dayElement.GetAttribute("date"), "yyyy-MM-dd", null, DateTimeStyles.AssumeLocal);

        entry.Updated = date;
        entry.Title = dayElement.GetAttribute("title");

        var reader = new StringReader(dayElement.GetSingleNodeValueOf("body/text()"));
        var content = new StringBuilder();
        var isDayContentEmpty = true;

        for (;;) {
          var line = reader.ReadLine();

          if (line == null)
            break;

          var match = entryHeaderRegex.Match(line);

          if (match.Success) {
            if (!isDayContentEmpty) {
              // yield current entry
              entry.Content = content.ToString();

              yield return entry;
            }

            // new entry
            entry = new Entry();

            var timestamp = match.Groups["timestamp"].Value;

            if (string.IsNullOrEmpty(timestamp))
              entry.Updated = date;
            else
              entry.Updated = UnixTimeStamp.ToLocalDateTime(int.Parse(timestamp));

            entry.Title = match.Groups["title"].Value;

            foreach (Capture c in match.Groups["category"].Captures) {
              entry.Categories.Add(c.Value);
            }

            content.Clear();
            isDayContentEmpty = false;
          }
          else {
            content.AppendLine(ConvertNotation(line));

            if (isDayContentEmpty && 0 < line.Trim().Length)
              isDayContentEmpty = false;
          }
        } // for each line

        // yield current entry
        entry.Content = content.ToString();

        yield return entry;
      } // for each day element
    }

    private static readonly Regex idCallRegex = new Regex(@"((d|b|a|f|h|i|r|graph):|g:[a-zA-Z0-9_\-]{3,}:)id:(?<id>[a-zA-Z0-9_\-]{3,})", RegexOptions.Singleline);

    private static string ConvertNotation(string line)
    {
      line = idCallRegex.Replace(line, delegate(Match m) {
        return "id:" + m.Groups["id"].Value; // XXX
      });

      return line;
    }
  }
}