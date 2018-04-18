using System;
using System.Linq;
using NUnit.Framework;

namespace Smdn.Applications.HatenaBlogTools {
  [TestFixture]
  public class HtmlDocumentTests {
    [TestCase("")]
    [TestCase("\n")]
    [TestCase("text")]
    public void TestToString_TextOnly(string input)
    {
      var doc = new HtmlDocument(input);

      Assert.IsEmpty(doc.Elements, "element count");
      Assert.AreEqual(input, doc.ToString(), "reconstructed document");
    }

    [TestCase("<p>")]
    [TestCase("text<p>")]
    [TestCase("<p>text")]
    [TestCase("text<p>text")]
    public void TestToString_WithNoAttributeElements(string input)
    {
      var doc = new HtmlDocument(input);

      Assert.AreEqual(1, doc.Elements.Count, "element count");
      Assert.AreEqual("p", doc.Elements[0].LocalName, "element local name #0");

      Assert.AreEqual(input, doc.ToString(), "reconstructed document");
    }

    [TestCase("</p>")]
    [TestCase("text</p>")]
    [TestCase("</p>text")]
    [TestCase("text</p>text")]
    public void TestToString_WithElementEnds(string input)
    {
      var doc = new HtmlDocument(input);

      Assert.AreEqual(0, doc.Elements.Count, "element count");

      Assert.AreEqual(input, doc.ToString(), "reconstructed document");
    }

    [TestCase("<p>text<p>")]
    [TestCase("text<p>text<p>")]
    [TestCase("text<p>text<p>text")]
    [TestCase("<p>text<p>text<p>text")]
    [TestCase("<p>text<p>text<p>text<p>")]
    public void TestToString_WithMultipleElements(string input)
    {
      var doc = new HtmlDocument(input);

      Assert.AreEqual(input, doc.ToString(), "reconstructed document");
    }

    [TestCase("<p class='parag' id='p1'>")]
    [TestCase("text<p class='parag' id='p1'>")]
    [TestCase("<p class='parag' id='p1'>text")]
    [TestCase("text<p class='parag' id='p1'>text")]
    [TestCase("text<p  class='parag' id='p1'>text")]
    [TestCase("text<p  class='parag'  id='p1'>text")]
    [TestCase("text<p  class='parag'  id='p1' >text")]
    [TestCase("text<p  class='parag'  id='p1'  >text")]
    [TestCase("text<p class=\"parag\" id=\"p1\">text")]
    [TestCase("text<p class=\"parag\" id=\"p1\"/>text")]
    [TestCase("text<p class=\"parag\" id=\"p1\" >text")]
    [TestCase("text<p class=\"parag\" id=\"p1\" />text")]
    public void TestToString_WithElementsAndAttributes(string input)
    {
      var doc = new HtmlDocument(input);

      Assert.AreEqual(1, doc.Elements.Count, "element count");
      Assert.AreEqual("p", doc.Elements[0].LocalName, "element local name #0");

      Assert.AreEqual(2, doc.Elements[0].Attributes.Count, "element #0 attribute count");

      Assert.AreEqual("class", doc.Elements[0].Attributes[0].Name, "element #0 attribute #0 name");
      Assert.AreEqual("parag", doc.Elements[0].Attributes[0].Value, "element #0 attribute #0 value");

      Assert.AreEqual("id", doc.Elements[0].Attributes[1].Name, "element #0 attribute #1 name");
      Assert.AreEqual("p1", doc.Elements[0].Attributes[1].Value, "element #0 attribute #1 value");

      Assert.AreEqual(input, doc.ToString(), "reconstructed document");
    }

    [TestCase("<e attr1='val1' attr2>")]
    [TestCase("<e attr1='val1'  attr2>")]
    [TestCase("<e attr1='val1' attr2 >")]
    [TestCase("<e attr1='val1'  attr2 >")]
    [TestCase("<e  attr1='val1'  attr2 >")]
    [TestCase("<e  attr1='val1'  attr2  >")]
    [TestCase("<e  attr1='val1'  attr2 />")]
    [TestCase("<e  attr1='val1'  attr2  />")]
    [TestCase("<e attr1=\"val1\" attr2>")]
    [TestCase("<e attr1=\"val1\" attr2/>")]
    [TestCase("<e attr1=\"val1\" attr2 >")]
    [TestCase("<e attr1=\"val1\" attr2 />")]
    public void TestToString_WithElementsAndEmptyAttributes1(string input)
    {
      var doc = new HtmlDocument(input);

      Assert.AreEqual(1, doc.Elements.Count, "element count");
      Assert.AreEqual("e", doc.Elements[0].LocalName, "element local name #0");

      Assert.AreEqual(2, doc.Elements[0].Attributes.Count, "element #0 attribute count");

      Assert.AreEqual("attr1", doc.Elements[0].Attributes[0].Name, "element #0 attribute #0 name");
      Assert.AreEqual("val1", doc.Elements[0].Attributes[0].Value, "element #0 attribute #0 value");

      Assert.AreEqual("attr2", doc.Elements[0].Attributes[1].Name, "element #0 attribute #1 name");
      Assert.AreEqual(null, doc.Elements[0].Attributes[1].Value, "element #0 attribute #1 value");

      Assert.AreEqual(input, doc.ToString(), "reconstructed document");
    }

    [TestCase("<e attr1 attr2='val2'>")]
    [TestCase("<e attr1  attr2='val2'>")]
    [TestCase("<e attr1 attr2='val2' >")]
    [TestCase("<e attr1  attr2='val2' >")]
    [TestCase("<e  attr1  attr2='val2' >")]
    [TestCase("<e  attr1  attr2='val2'  >")]
    [TestCase("<e  attr1  attr2='val2' />")]
    [TestCase("<e  attr1  attr2='val2'  />")]
    [TestCase("<e attr1 attr2=\"val2\">")]
    [TestCase("<e attr1 attr2=\"val2\"/>")]
    [TestCase("<e attr1 attr2=\"val2\" >")]
    [TestCase("<e attr1 attr2=\"val2\" />")]
    public void TestToString_WithElementsAndEmptyAttributes2(string input)
    {
      var doc = new HtmlDocument(input);

      Assert.AreEqual(1, doc.Elements.Count, "element count");
      Assert.AreEqual("e", doc.Elements[0].LocalName, "element local name #0");

      Assert.AreEqual(2, doc.Elements[0].Attributes.Count, "element #0 attribute count");

      Assert.AreEqual("attr1", doc.Elements[0].Attributes[0].Name, "element #0 attribute #0 name");
      Assert.AreEqual(null, doc.Elements[0].Attributes[0].Value, "element #0 attribute #0 value");

      Assert.AreEqual("attr2", doc.Elements[0].Attributes[1].Name, "element #0 attribute #1 name");
      Assert.AreEqual("val2", doc.Elements[0].Attributes[1].Value, "element #0 attribute #1 value");

      Assert.AreEqual(input, doc.ToString(), "reconstructed document");
    }
  }
}