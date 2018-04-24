using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace Smdn.Applications.HatenaBlogTools {
  [TestFixture]
  public class FixMixedContentTests {
    private void WithInputFile(string input, Action<FileInfo> actionWithFile)
    {
      var file = new FileInfo(Path.Combine(TestContext.CurrentContext.TestDirectory, "input.txt"));

      try {
        File.WriteAllText(file.FullName, input, Encoding.UTF8);

        actionWithFile(file);
      }
      finally {
        if (file.Exists)
          file.Delete();
      }
    }

    private string WithOutputFile(Action<FileInfo> actionWithFile)
    {
      var file = new FileInfo(Path.Combine(TestContext.CurrentContext.TestDirectory, "output.txt"));

      try {
        actionWithFile(file);

        return File.ReadAllText(file.FullName, Encoding.UTF8);
      }
      finally {
        if (file.Exists)
          file.Delete();
      }
    }

    private string EditLocalContent(string input, string[] args)
    {
      string ret = null;

      WithInputFile(input, inputFile => {
        ret = WithOutputFile(outputFile => {
          var mergedArgs = new List<string>() {
            "--input-content",
            inputFile.FullName,
            "--output-content",
            outputFile.FullName,
          };

          if (args != null)
            mergedArgs.AddRange(args);

          (new FixMixedContent()).Run(mergedArgs.ToArray());
        });
      });

      return ret;
    }

    [Test]
    public void TestEditLocalContent_NoModifications()
    {
      const string input = @"
http://example.com/
";
      const string expectedOutput = @"
http://example.com/
";

      var output = EditLocalContent(input, null);

      Assert.AreEqual(expectedOutput, output);
    }

    [Test]
    public void TestEditLocalContent_FixBlogUrl()
    {
      const string input = @"
http://example.com/
http://example.net/

[http://example.com/]
[http://example.com/:title]

<a href=""http://example.com/"">
<a href=""http://example.net/"">

<script src=""http://example.com/"">
";
      const string expectedOutput = @"
https://example.com/
http://example.net/

[https://example.com/]
[https://example.com/:title]

<a href=""https://example.com/"">
<a href=""http://example.net/"">

<script src=""http://example.com/"">
";

      var output = EditLocalContent(input, new[] { "--fix-blog-url", "--custom-domain", "example.com" });

      Assert.AreEqual(expectedOutput, output);
    }

    [Test]
    public void TestEditLocalContent_FixMixedContent()
    {
      const string input = @"
http://example.com/
http://example.net/

<a href=""http://example.com/"">
<a href=""http://example.net/"">

<script src=""http://example.com/"">
<script src=""http://example.net/"">
";
      const string expectedOutput = @"
http://example.com/
http://example.net/

<a href=""http://example.com/"">
<a href=""http://example.net/"">

<script src=""https://example.com/"">
<script src=""https://example.net/"">
";

      var output = EditLocalContent(input, new[] { "--fix-mixed-content" });

      Assert.AreEqual(expectedOutput, output);
    }

    [Test]
    public void TestEditLocalContent_FixMixedContent_IncludeDomain()
    {
      const string input = @"
<script src=""http://example.com/"">
<script src=""http://example.net/"">
<script src=""http://example.org/"">
";
      const string expectedOutput = @"
<script src=""https://example.com/"">
<script src=""https://example.net/"">
<script src=""http://example.org/"">
";

      var output = EditLocalContent(input, new[] { "--fix-mixed-content", "--include-domain", "example.com", "--include-domain", "example.net" });

      Assert.AreEqual(expectedOutput, output);
    }

    [Test]
    public void TestEditLocalContent_FixMixedContent_ExcludeDomain()
    {
      const string input = @"
<script src=""http://example.com/"">
<script src=""http://example.net/"">
<script src=""http://example.org/"">
";
      const string expectedOutput = @"
<script src=""http://example.com/"">
<script src=""http://example.net/"">
<script src=""https://example.org/"">
";

      var output = EditLocalContent(input, new[] { "--fix-mixed-content", "--exclude-domain", "example.com", "--exclude-domain", "example.net" });

      Assert.AreEqual(expectedOutput, output);
    }

    [Test]
    public void TestEditLocalContent_FixMixedContentAndBlogUrl()
    {
      const string input = @"
http://example.com/
http://example.net/

<a href=""http://example.com/"">
<a href=""http://example.net/"">

<script src=""http://example.com/"">
<script src=""http://example.net/"">
";
      const string expectedOutput = @"
https://example.com/
http://example.net/

<a href=""https://example.com/"">
<a href=""http://example.net/"">

<script src=""https://example.com/"">
<script src=""https://example.net/"">
";

      var output = EditLocalContent(input, new[] { "--fix-mixed-content", "--fix-blog-url", "--custom-domain", "example.com" });

      Assert.AreEqual(expectedOutput, output);
    }

    [Test]
    public void TestEditLocalContent_Hatena_FixMixedContent()
    {
      const string input = @"
<figure class=""figure-image figure-image-fotolife mceNonEditable"" title=""smdn"">
<p><img class=""hatena-fotolife"" src=""http://cdn-ak.f.st-hatena.com/images/fotolife/s/smdn/20131114/20131114222653.png"" alt=""smdn favicon"" /></p>
<figcaption>smdn</figcaption>
</figure>
<p><iframe class=""embed-card embed-blogcard"" style=""display: block; width: 100%; height: 190px; max-width: 500px; margin: 10px 0px;"" title=""http://smdn.hatenablog.jp/entry/2013/09/25/130552"" src=""http://smdn.hatenablog.jp/embed/2013/09/25/130552"" frameborder=""0"" scrolling=""no""></iframe><cite class=""hatena-citation""><a href=""http://smdn.hatenablog.jp/entry/2013/09/25/130552"">smdn.hatenablog.jp</a></cite></p>
<p><iframe class=""embed-card embed-webcard"" style=""display: block; width: 100%; height: 155px; max-width: 500px; margin: 10px 0px;"" title=""smdn:総武ソフトウェア推進所"" src=""http://hatenablog-parts.com/embed?url=http%3A%2F%2Fsmdn.jp%2F"" frameborder=""0"" scrolling=""no""></iframe><cite class=""hatena-citation""><a href=""http://smdn.jp/"">smdn.jp</a></cite></p>
";
      const string expectedOutput = @"
<figure class=""figure-image figure-image-fotolife mceNonEditable"" title=""smdn"">
<p><img class=""hatena-fotolife"" src=""https://cdn-ak.f.st-hatena.com/images/fotolife/s/smdn/20131114/20131114222653.png"" alt=""smdn favicon"" /></p>
<figcaption>smdn</figcaption>
</figure>
<p><iframe class=""embed-card embed-blogcard"" style=""display: block; width: 100%; height: 190px; max-width: 500px; margin: 10px 0px;"" title=""http://smdn.hatenablog.jp/entry/2013/09/25/130552"" src=""https://smdn.hatenablog.jp/embed/2013/09/25/130552"" frameborder=""0"" scrolling=""no""></iframe><cite class=""hatena-citation""><a href=""http://smdn.hatenablog.jp/entry/2013/09/25/130552"">smdn.hatenablog.jp</a></cite></p>
<p><iframe class=""embed-card embed-webcard"" style=""display: block; width: 100%; height: 155px; max-width: 500px; margin: 10px 0px;"" title=""smdn:総武ソフトウェア推進所"" src=""https://hatenablog-parts.com/embed?url=http%3A%2F%2Fsmdn.jp%2F"" frameborder=""0"" scrolling=""no""></iframe><cite class=""hatena-citation""><a href=""http://smdn.jp/"">smdn.jp</a></cite></p>
";

      var output = EditLocalContent(input, new[] { "--fix-mixed-content" });

      Assert.AreEqual(expectedOutput, output);
    }

    [Test]
    public void TestEditLocalContent_Hatena_FixBlogUrl()
    {
      const string input = @"
<figure class=""figure-image figure-image-fotolife mceNonEditable"" title=""smdn"">
<p><img class=""hatena-fotolife"" src=""http://cdn-ak.f.st-hatena.com/images/fotolife/s/smdn/20131114/20131114222653.png"" alt=""smdn favicon"" /></p>
<figcaption>smdn</figcaption>
</figure>
<p><iframe class=""embed-card embed-blogcard"" style=""display: block; width: 100%; height: 190px; max-width: 500px; margin: 10px 0px;"" title=""http://smdn.hatenablog.jp/entry/2013/09/25/130552"" src=""http://smdn.hatenablog.jp/embed/2013/09/25/130552"" frameborder=""0"" scrolling=""no""></iframe><cite class=""hatena-citation""><a href=""http://smdn.hatenablog.jp/entry/2013/09/25/130552"">smdn.hatenablog.jp</a></cite></p>
<p><iframe class=""embed-card embed-webcard"" style=""display: block; width: 100%; height: 155px; max-width: 500px; margin: 10px 0px;"" title=""smdn:総武ソフトウェア推進所"" src=""http://hatenablog-parts.com/embed?url=http%3A%2F%2Fsmdn.jp%2F"" frameborder=""0"" scrolling=""no""></iframe><cite class=""hatena-citation""><a href=""http://smdn.jp/"">smdn.jp</a></cite></p>
";
      const string expectedOutput = @"
<figure class=""figure-image figure-image-fotolife mceNonEditable"" title=""smdn"">
<p><img class=""hatena-fotolife"" src=""http://cdn-ak.f.st-hatena.com/images/fotolife/s/smdn/20131114/20131114222653.png"" alt=""smdn favicon"" /></p>
<figcaption>smdn</figcaption>
</figure>
<p><iframe class=""embed-card embed-blogcard"" style=""display: block; width: 100%; height: 190px; max-width: 500px; margin: 10px 0px;"" title=""http://smdn.hatenablog.jp/entry/2013/09/25/130552"" src=""http://smdn.hatenablog.jp/embed/2013/09/25/130552"" frameborder=""0"" scrolling=""no""></iframe><cite class=""hatena-citation""><a href=""https://smdn.hatenablog.jp/entry/2013/09/25/130552"">smdn.hatenablog.jp</a></cite></p>
<p><iframe class=""embed-card embed-webcard"" style=""display: block; width: 100%; height: 155px; max-width: 500px; margin: 10px 0px;"" title=""smdn:総武ソフトウェア推進所"" src=""http://hatenablog-parts.com/embed?url=http%3A%2F%2Fsmdn.jp%2F"" frameborder=""0"" scrolling=""no""></iframe><cite class=""hatena-citation""><a href=""http://smdn.jp/"">smdn.jp</a></cite></p>
";

      var output = EditLocalContent(input, new[] { "--fix-blog-url", "--custom-domain", "smdn.hatenablog.jp" });

      Assert.AreEqual(expectedOutput, output);
    }
  }
}