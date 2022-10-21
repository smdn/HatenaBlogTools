// SPDX-FileCopyrightText: 2013 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#undef RETRIEVE_COMMENTS

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Smdn.HatenaBlogTools.AtomPublishingProtocol;
using Smdn.HatenaBlogTools.HatenaBlog;
using Smdn.IO;
using Smdn.Xml.Linq;

namespace Smdn.HatenaBlogTools;

internal partial class DumpAllEntries : CliBase {
  private enum OutputFormat {
    Atom,
    AtomPostData,
    AtomBlogger,
    MovableType,
    HatenaDiary,
    EntryFile,

    Default = Atom,
  }

#if RETRIEVE_COMMENTS
  private class Comment {
    public string Content;
    public string Author;
    public string Url;
    public DateTime Date;
  }
#endif

  protected override string GetDescription() => "すべてのブロク記事をダンプします。";

  protected override string GetUsageExtraMandatoryOptions() => "[--format [hatena|mt|atom]] [出力ファイル名|出力ディレクトリ名|-]";

  protected override IEnumerable<string> GetUsageExtraOptionDescriptions()
  {
#if RETRIEVE_COMMENTS
    yield return "-comment : dump comments posted on entry"
#endif
    yield return "--format [hatena|mt|atom|atom-post|atom-blogger] : 出力形式を指定します";
    yield return "  --format hatena       : はてなダイアリー日記データ形式";
    yield return "  --format mt           : Movable Type形式";
    yield return "  --format atom         : Atomフィード形式(はてなブログAPIで取得できる全内容)";
    yield return "  --format atom-post    : Atomフィード形式(はてなブログAPIで取得できる内容のうち、投稿データのみ抽出)";
    yield return "  --format atom-blogger : Atomフィード形式(Blogger用フォーマット)";
    yield return "  --format entry-file   : 記事ごとに個別のファイルに出力";
    yield return "　(省略した場合は、'--format atom'を指定した場合と同じ形式で出力します)";
    yield return string.Empty;
    yield return "--exclude-category <カテゴリ>  : 指定された<カテゴリ>を除外してダンプします(複数指定可)";
    yield return "--include-category <カテゴリ>  : 指定された<カテゴリ>のみを抽出してダンプします(複数指定可)";
    yield return string.Empty;
    yield return "--exclude-notation [hatena|md|html] : 指定された記法の記事を除外してダンプします(複数指定可)";
    yield return "--include-notation [hatena|md|html] : 指定された記法の記事のみを抽出してダンプします(複数指定可)";
    yield return string.Empty;
#if false
    yield return "Blogger用フォーマット(atom-blogger)のオプション:";
    yield return "  --blogger-domain <ドメイン> : Bloggerのブログドメイン(***.blogspot.com)を指定します(省略可)";
    yield return "  --blogger-id <ブログID>     : BloggerのブログIDを指定します(省略可)";
    yield return "                                ブログドメインとIDを指定した場合は、各記事に指定されているカスタムURLと";
    yield return "                                はてなブログの設定の一部をBloggerの設定に変換します";
    yield return "";
#endif
    yield return "[出力ファイル名|出力ディレクトリ名|-]";
    yield return "                          : ダンプした内容の保存先ファイル名/ディレクトリ名を指定します";
    yield return "                            省略した場合、- を指定した場合は標準出力に書き込みます";
    yield return "                            --formatにentry-fileを指定した場合、出力ディレクトリ名は省略できません";
  }

  public void Run(string[] args)
  {
    if (!ParseCommonCommandLineArgs(ref args, out var credential))
      return;

#if RETRIEVE_COMMENTS
    bool retrieveComments = false;
#endif
    var categoriesToExclude = new HashSet<string>(StringComparer.Ordinal);
    var categoriesToInclude = new HashSet<string>(StringComparer.Ordinal);
    var notationsToExclude = new HashSet<string>(StringComparer.Ordinal);
    var notationsToInclude = new HashSet<string>(StringComparer.Ordinal);
    var outputFormat = OutputFormat.Default;
    string outputPath = "-";
#if false
    string bloggerDomain = null;
    string bloggerId = null;
#endif

    string NotationNameToContentType(string notation)
    {
      switch (notation) {
        case "hatena": return EntryContentType.HatenaSyntax;
        case "md": return EntryContentType.Markdown;
        case "html": return EntryContentType.Html;
        default:
          Usage("unsupported notation: {0}", notation);
          throw new AbortCommandException();
      }
    }

    for (var i = 0; i < args.Length; i++) {
      switch (args[i]) {
        case "--format":
        case "-format":
          var format = args[++i];

          switch (format) {
            case "hatena":
              outputFormat = OutputFormat.HatenaDiary;
              break;

            case "mt":
              outputFormat = OutputFormat.MovableType;
              break;

            case "atom":
              outputFormat = OutputFormat.Atom;
              break;

            case "atom-post":
              outputFormat = OutputFormat.AtomPostData;
              break;

            case "atom-blogger":
              outputFormat = OutputFormat.AtomBlogger;
              break;

            case "entry-file":
              outputFormat = OutputFormat.EntryFile;
              break;

            default:
              Usage("unsupported format: {0}", format);
              break;
          }

          break;

        case "--exclude-category":
        case "-excat":
          categoriesToExclude.Add(args[++i]);
          break;

        case "--include-category":
        case "-incat":
          categoriesToInclude.Add(args[++i]);
          break;

        case "--exclude-notation":
          notationsToExclude.Add(NotationNameToContentType(args[++i]));
          break;

        case "--include-notation":
          notationsToInclude.Add(NotationNameToContentType(args[++i]));
          break;

#if false
        case "--blogger-domain":
          bloggerDomain = args[++i];
          break;

        case "--blogger-id":
          bloggerId = args[++i];
          break;
#endif

#if RETRIEVE_COMMENTS
        case "-comment":
          retrieveComments = true;
          break;
#endif

        default:
          outputPath = args[i];
          break;
      }
    }

    if (outputFormat == OutputFormat.EntryFile) {
      if (outputPath == null) {
        Usage("--format entry-fileでは、出力先ディレクトリが指定されている必要があります");
        return;
      }

      if (outputPath == "-") {
        Usage("--format entry-fileでは、出力先に標準出力を指定することはできません");
        return;
      }
    }

    if (0 < categoriesToExclude.Count && 0 < categoriesToInclude.Count) {
      Usage("--exclude-categoryと--include-categoryを同時に指定することはできません");
      return;
    }

    if (0 < notationsToExclude.Count && 0 < notationsToInclude.Count) {
      Usage("--exclude-notationと--include-notationを同時に指定することはできません");
      return;
    }

    if (credential is null)
      throw new InvalidOperationException("credential not set");

    if (!Login(credential, out var hatenaBlog))
      return;

    bool EntryCategoryPredicate(PostedEntry entry)
    {
      if (0 < categoriesToExclude.Count && entry.Categories.Overlaps(categoriesToExclude)) {
        Console.Error.WriteLine("対象カテゴリを含むため除外します: ({0})", string.Join(", ", entry.Categories));
        return false;
      }

      if (0 < categoriesToInclude.Count && !entry.Categories.Overlaps(categoriesToInclude)) {
        Console.Error.WriteLine("対象カテゴリを含まないため除外します ({0})", string.Join(", ", entry.Categories));
        return false;
      }

      return true;
    }

    bool EntryNotationPredicate(PostedEntry entry)
    {
      if (0 < notationsToExclude.Count && notationsToExclude.Contains(entry.ContentType)) {
        Console.Error.WriteLine("対象外の記法で記述されているため除外します: ({0})", entry.ContentType);
        return false;
      }

      if (0 < notationsToInclude.Count && !notationsToInclude.Contains(entry.ContentType)) {
        Console.Error.WriteLine("対象の記法で記述されていないため除外します: ({0})", entry.ContentType);
        return false;
      }

      return true;
    }

    bool EntryPredicate(PostedEntry entry)
    {
      Console.Error.Write($"{entry.EntryUri} \"{entry.Title}\" : ");

      if (EntryCategoryPredicate(entry) && EntryNotationPredicate(entry)) {
        Console.Error.WriteLine("完了");
        return true;
      }

      return false;
    }

    var outputDocument = DumpEntries(hatenaBlog, EntryPredicate, out var entries);

    if (outputDocument == null)
      return;

    // 結果を保存
    Console.Error.WriteLine("結果を保存しています");

    if (outputFormat == OutputFormat.EntryFile) {
      OutputEntriesToIndividualFiles(
        outputDocument,
        entries,
        hatenaBlog.BlogId,
        outputPath
      );
    }
    else {
      using var outputStream = outputPath == "-"
        ? Console.OpenStandardOutput()
        : new FileStream(outputPath, FileMode.Create, FileAccess.Write);
      void OutputWithFormatter(FormatterBase formatter) => formatter.Format(entries, outputStream);

      switch (outputFormat) {
        case OutputFormat.Atom:
        case OutputFormat.AtomPostData:
          OutputDocument(outputDocument, outputFormat, outputStream);
          break;

        case OutputFormat.HatenaDiary:
          OutputWithFormatter(new HatenaDiaryFormatter(/*retrieveComments*/));
          break;

        case OutputFormat.MovableType:
          OutputWithFormatter(new MovableTypeFormatter(/*retrieveComments*/));
          break;

        case OutputFormat.AtomBlogger:
          OutputWithFormatter(
            new BloggerFormatter(
              blogTitle: hatenaBlog.BlogTitle
#if false
                blogDomain: bloggerDomain,
                blogId: bloggerId
              /*, retrieveComments*/
#endif
            )
          );
          break;

        default:
          throw new NotSupportedException($"unsupported format: {outputFormat}");
      }
    }

    Console.Error.WriteLine("完了");
  }

#pragma warning disable SA1305
  private static readonly XNamespace nsXLink = "http://www.w3.org/1999/xlink";
#pragma warning restore SA1305

  private static void OutputEntriesToIndividualFiles(
    XDocument document,
    IReadOnlyList<PostedEntry> entries,
    string blogId,
    string outputDirectory
  )
  {
    const string defaultOutputFileExtension = ".txt";

    Directory.CreateDirectory(outputDirectory);

    foreach (var (entry, index) in entries.Select(static (entry, index) => (entry, index))) {
      // replace: '/' -> '-', invalid chars -> '_'
      var suffix = entry.EntryUri is null
        ? index.ToString("D", CultureInfo.InvariantCulture) // use entry index instead
        : string.Join(
            "-",
            (entry.Id ?? entry.EntryUri).LocalPath.Substring(1).Split('/').Select(seg => PathUtils.ReplaceInvalidFileNameChars(seg, "_"))
          );
      var fileName = $"{entry.DatePublished.Year:D4}{entry.DatePublished.Month:D2}{entry.DatePublished.Day:D2}_{suffix}";
      var fileExtension = entry.ContentType is null
        ? defaultOutputFileExtension
        : EntryContentType.GetFileExtension(entry.ContentType) ?? defaultOutputFileExtension;

      var entryFilePath = Path.Combine(
        outputDirectory,
        Path.ChangeExtension(fileName, fileExtension)
      );

      File.WriteAllText(entryFilePath, entry.Content);

      // set file timestamp
      File.SetCreationTimeUtc(entryFilePath, entry.DatePublished.UtcDateTime);
      File.SetLastWriteTimeUtc(entryFilePath, (entry.DateUpdated ?? DateTimeOffset.Now).UtcDateTime);

      // edit metadata
      var elementEntry = document
        ?.Root
        ?.Elements(AtomPub.Namespaces.Atom + "entry")
        ?.FirstOrDefault(e =>
          string.Equals(e.Element(AtomPub.Namespaces.Atom + "id")?.Value, entry.Id?.AbsoluteUri, StringComparison.Ordinal)
        );

      if (elementEntry != null) {
        // replace atom:content
        var elementEntryContent = elementEntry.Element(AtomPub.Namespaces.Atom + "content");

        if (elementEntryContent is not null) {
          elementEntryContent.AddAfterSelf(
            new XElement(
              AtomPub.Namespaces.Atom + "content",
              new XAttribute("type", entry.ContentType ?? EntryContentType.Default),
              new XAttribute(nsXLink + "type", "simple"),
              new XAttribute(nsXLink + "href", new Uri("file://" + Path.GetFullPath(entryFilePath)))
            )
          );

          elementEntryContent.Remove();
        }

        // remove hatena:formatted-content
        elementEntry.Element(AtomPub.Namespaces.Hatena + "formatted-content")?.Remove();
      }
    }

    document.Root?.Add(
      new XAttribute(XNamespace.Xmlns + "xlink", nsXLink.NamespaceName)
    );

    var metadataFilePath = Path.Combine(
      outputDirectory,
      $"entry-metadata.{blogId}.xml"
    );

    document.Save(metadataFilePath);
  }

  private static void OutputDocument(
    XDocument document,
    OutputFormat outputFormat,
    Stream outputStream
  )
  {
    switch (outputFormat) {
      case OutputFormat.AtomPostData:
        var elementsEntry = document.Root?.Elements(AtomPub.Namespaces.Atom + "entry");

        if (elementsEntry is not null) {
          elementsEntry.Elements(AtomPub.Namespaces.Atom + "id").Remove();
          elementsEntry.Elements(AtomPub.Namespaces.Atom + "link").Remove();
          elementsEntry.Elements(AtomPub.Namespaces.App + "edited").Remove();
          elementsEntry.Elements(AtomPub.Namespaces.Hatena + "formatted-content").Remove();
        }

        goto case OutputFormat.Atom;

      case OutputFormat.Atom:
        document.Save(outputStream);
        break;

      default:
        throw new NotSupportedException($"unsupported format: {outputFormat}");
    }
  }

  private static XDocument DumpEntries(
    HatenaBlogAtomPubClient hatenaBlog,
    Predicate<PostedEntry> entryPredicate,
    out IReadOnlyList<PostedEntry> entries
  )
  {
    var filteredEntries = new List<PostedEntry>();

    XDocument outputDocument = null;

    Console.Error.WriteLine("エントリをダンプ中 ...");

    hatenaBlog.EnumerateEntries((postedEntry, entryElement) => {
      if (outputDocument == null && entryElement.Document is not null) {
        // オリジナルのレスポンス文書からlink[@rel=first/next], entry以外の要素をコピーしてヘッダ部を構築する
        outputDocument = new XDocument(entryElement.Document);

        outputDocument
          .Root
          ?.Elements(AtomPub.Namespaces.Atom + "entry")
          ?.Remove();

        outputDocument
          .Root
          ?.Elements(AtomPub.Namespaces.Atom + "link")
          ?.Where(e => e.HasAttributeWithValue("rel", "first") || e.HasAttributeWithValue("rel", "next"))
          ?.Remove();
      }

      if (!entryPredicate(postedEntry))
        return; // continue

      outputDocument?.Root?.Add(new XElement(entryElement));

      filteredEntries.Add(postedEntry);
    });

    entries = filteredEntries;

    return outputDocument;
  }

#if RETRIEVE_COMMENTS
  /// <remarks>コメントはJavaScriptによって動的に読み込まれているので、この方法では取得できない</remarks>
  private static IEnumerable<Comment> RetrieveComments(string entryUrl)
  {
    var doc = new XmlDocument();

    Console.Error.Write("{0} のコメントを取得中 ... ", entryUrl);

    using (var sgmlReader = new Sgml.SgmlReader()) {
      sgmlReader.Href = entryUrl;
      sgmlReader.CaseFolding = Sgml.CaseFolding.ToLower;

      doc.Load(sgmlReader);

      System.Threading.Thread.Sleep(500);
    }

    //var contentNode = doc.GetElementById("content");
    var contentNode = doc.SelectSingleNode("//*[@id = 'content']");

    foreach (XmlElement commentRootElement in contentNode.SelectNodes(".//*[contains(@class, 'entry-comment')]")) {
      var comment = new Comment();

      foreach (XmlNode commentChildNode in commentRootElement.ChildNodes) {
        if (commentChildNode.NodeType != XmlNodeType.Element)
          continue;

        var commentChildElement = (XmlElement)commentChildNode;

        switch (commentChildElement.GetAttribute("class")) {
          case "comment-user-name":
            /*
             * <!-- hatena user -->
             * <e class="comment-user-name">
             *   <a class="comment-user-id" href="http://blog.hatena.ne.jp/hatenaid/">
             *     <span class="comment-nickname" data-user-name="hatenaid">
             *       id:hatenaid
             *     </span>
             *   </a>
             * </e>
             * <!-- name with website -->
             * <e class="comment-user-name">
             *   name
             *   <a class="icon-website" href="http://example.com/" />
             * </e>
             * <!-- name only -->
             * <e class="comment-user-name">
             *   name
             * </e>
             */
            comment.Author = commentChildElement.InnerText.Trim();
            comment.Url = commentChildElement.GetSingleNodeValueOf(".//@href");
            break;

          case "comment-content":
            /*
             * <e class="comment-content">
             *   <p>comment-html</p>
             * </e>
             */
            comment.Content = commentChildElement.FirstChild.InnerXml;
            break;

          case "comment-metadata":
            /*
             * <e class="comment-metadata">
             *   <time data-epoch="1387283661000" />
             * </e>
             */
            comment.Date = DateTimeOffset.FromUnixTime(commentChildElement.GetSingleNodeValueOf("time/@data-epoch", long.Parse) / 1000).ToLocalTime();
            break;
        }
      }

      yield return comment;
    }

    Console.Error.WriteLine("完了");
  }
#endif
}
