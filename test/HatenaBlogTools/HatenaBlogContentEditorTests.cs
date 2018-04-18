using System;
using System.Linq;
using NUnit.Framework;

namespace Smdn.Applications.HatenaBlogTools {
  [TestFixture]
  public class HatenaBlogContentEditorTests {
    [TestCase("<img src=\"http://example.com/test.dat\">",
              "<img src=\"https://example.com/test.dat\">")]
    [TestCase("<img src=\"http://foo.invalid/\">",
              "<img src=\"https://foo.invalid/\">")]
    [TestCase("<img src=\"http://foo.bar.invalid/\">",
              "<img src=\"https://foo.bar.invalid/\">")]

    [TestCase("<img src=\" http://example.com/test.dat\">",
              "<img src=\" https://example.com/test.dat\">")]
    [TestCase("<img src=\"  http://example.com/test.dat\">",
              "<img src=\"  https://example.com/test.dat\">")]
    [TestCase("<img src=\"  http://example.com/test.dat \">",
              "<img src=\"  https://example.com/test.dat \">")]

    [TestCase("<img src=http://example.com/test.dat>",
              "<img src=https://example.com/test.dat>")]

    [TestCase("<img src='http://example.com/test.dat'>",
              "<img src='https://example.com/test.dat'>")]

    [TestCase("<img src=http://example.com/test.dat/>",
              "<img src=https://example.com/test.dat/>")]

    [TestCase("<img src=http://example.com/test.dat />",
              "<img src=https://example.com/test.dat />")]

    [TestCase("<img src = http://example.com/test.dat>",
              "<img src = https://example.com/test.dat>")]

    [TestCase("<img\n  src=\n  http://example.com/test.dat>",
              "<img\n  src=\n  https://example.com/test.dat>")]

    [TestCase("<img srcset=\"http://example.com/test.dat 1x,\nhttp://example.com/test.dat 2x\">",
              "<img srcset=\"https://example.com/test.dat 1x,\nhttps://example.com/test.dat 2x\">")]

    [TestCase("<source src=\"http://example.com/test.dat\">",
              "<source src=\"https://example.com/test.dat\">")]
    [TestCase("<source srcset=\"http://example.com/test.dat 1x,\nhttp://example.com/test.dat 2x\">",
              "<source srcset=\"https://example.com/test.dat 1x,\nhttps://example.com/test.dat 2x\">")]

    [TestCase("<script src=\"http://example.com/test.dat\">",
              "<script src=\"https://example.com/test.dat\">")]

    [TestCase("<video src=\"http://example.com/test.dat\">",
              "<video src=\"https://example.com/test.dat\">")]

    [TestCase("<audio src=\"http://example.com/test.dat\">",
              "<audio src=\"https://example.com/test.dat\">")]

    [TestCase("<iframe src=\"http://example.com/test.dat\">",
              "<iframe src=\"https://example.com/test.dat\">")]

    [TestCase("<embed src=\"http://example.com/test.dat\">",
              "<embed src=\"https://example.com/test.dat\">")]

    [TestCase("<link rel=\"stylesheet\" href=\"http://example.com/test.dat\">",
              "<link rel=\"stylesheet\" href=\"https://example.com/test.dat\">")]

    [TestCase("<form action=\"http://example.com/test.dat\">",
              "<form action=\"https://example.com/test.dat\">")]

    [TestCase("<object data=\"http://example.com/test.dat\">",
              "<object data=\"https://example.com/test.dat\">")]
    public void TestFixMixedContentReferences(string input, string expectedResult)
    {
      var editor = new HatenaBlogContentEditor(input);

      editor.FixMixedContentReferences();

      Assert.AreEqual(expectedResult, editor.ToString(), "modified content");
    }

    [TestCase("<img src=\"https://example.com/test.dat\">",
              "<img src=\"https://example.com/test.dat\">")]

    [TestCase("<img srcset=\"https://example.com/test.dat 1x,\nhttp://example.com/test.dat 2x\">",
              "<img srcset=\"https://example.com/test.dat 1x,\nhttps://example.com/test.dat 2x\">")]

    [TestCase("<img srcset=\"https://example.com/test.dat 1x,\nhttps://example.com/test.dat 2x\">",
              "<img srcset=\"https://example.com/test.dat 1x,\nhttps://example.com/test.dat 2x\">")]
    public void TestFixMixedContentReferences_HttpsReference(string input, string expectedResult)
    {
      var editor = new HatenaBlogContentEditor(input);

      editor.FixMixedContentReferences();

      Assert.AreEqual(expectedResult, editor.ToString(), "modified content");
    }

    [TestCase("<img src=\"//example.com/test.dat\">")]
    public void TestFixMixedContentReferences_ProtocolRelativeReference(string input)
    {
      var editor = new HatenaBlogContentEditor(input);

      editor.FixMixedContentReferences();

      Assert.AreEqual(input, editor.ToString(), "modified content");
    }

    [TestCase("<img src=\"/test.dat\">")]
    [TestCase("<img src=\"./test.dat\">")]
    [TestCase("<img src=\"test.dat\">")]
    public void TestFixMixedContentReferences_RelativeReference(string input)
    {
      var editor = new HatenaBlogContentEditor(input);

      editor.FixMixedContentReferences();

      Assert.AreEqual(input, editor.ToString(), "modified content");
    }

    [TestCase("<img src=\"ftps://example.com/test.dat\">")]
    public void TestFixMixedContentReferences_NonHttpReference(string input)
    {
      var editor = new HatenaBlogContentEditor(input);

      editor.FixMixedContentReferences();

      Assert.AreEqual(input, editor.ToString(), "modified content");
    }

    [TestCase("<link rel=\"canonical\" href=\"http://example.com/test.dat\">")]
    public void TestFixMixedContentReferences_NonStylesheetLink(string input)
    {
      var editor = new HatenaBlogContentEditor(input);

      editor.FixMixedContentReferences();

      Assert.AreEqual(input, editor.ToString(), "modified content");
    }

    [TestCase("<a href=\"http://example.com/test.dat\">")]
    [TestCase("<a src=\"http://example.com/test.dat\">")]
    [TestCase("<link src=\"http://example.com/test.dat\">")]
    public void TestFixMixedContentReferences_NonEmbeddedReferences(string input)
    {
      var editor = new HatenaBlogContentEditor(input);

      editor.FixMixedContentReferences();

      Assert.AreEqual(input, editor.ToString(), "modified content");
    }

    [TestCase("http://example.com/test.dat")]
    [TestCase("img src=http://example.com/test.dat")]
    [TestCase("&ltimg src=http://example.com/test.dat&gt;")]
    public void TestFixMixedContentReferences_ContainsNoReferences(string input)
    {
      var editor = new HatenaBlogContentEditor(input);

      editor.FixMixedContentReferences();

      Assert.AreEqual(input, editor.ToString(), "modified content");
    }



    [TestCase("http://example.com/",
              "https://example.com/")]
    [TestCase("http://example.net/",
              "https://example.net/")]
    [TestCase("http://example.com/ http://example.net/",
              "https://example.com/ https://example.net/")]
    [TestCase("http://example.com/test.dat",
              "https://example.com/test.dat")]
    [TestCase("<p>welcome to http://example.com/<p>",
              "<p>welcome to https://example.com/<p>")]
    [TestCase("welcome to <a href=\"http://example.com/\">http://example.com/</a>",
              "welcome to <a href=\"https://example.com/\">https://example.com/</a>")]
    [TestCase("welcome to [http://example.com/:title=my blog]",
              "welcome to [https://example.com/:title=my blog]")]
    public void TestReplaceBlogUrlToHttps(string input, string expectedResult)
    {
      var editor = new HatenaBlogContentEditor(input);

      editor.ReplaceBlogUrlToHttps(new[] {"example.com", "example.net"});

      Assert.AreEqual(expectedResult, editor.ToString(), "modified content");
    }

    [TestCase("http://example.net/")]
    [TestCase("<a href=\"http://example.net/\">http://example.net/</a>")]
    public void TestReplaceBlogUrlToHttps_NonBlogUrl(string input)
    {
      var editor = new HatenaBlogContentEditor(input);

      editor.ReplaceBlogUrlToHttps(new[] { "example.com" });

      Assert.AreEqual(input, editor.ToString(), "modified content");
    }
  }
}