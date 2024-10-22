using System;
using System.Linq;
using NUnit.Framework;

namespace Smdn.HatenaBlogTools.Html;

[TestFixture]
public class HtmlDocumentTests {
  [TestCase("")]
  [TestCase("\n")]
  [TestCase("text")]
  public void TestToString_TextOnly(string input)
  {
    var doc = new HtmlDocument(input);

    Assert.That(doc.Elements, Is.Empty, "element count");
    Assert.That(doc.ToString(), Is.EqualTo(input), "reconstructed document");
  }

  [TestCase("<p>")]
  [TestCase("text<p>")]
  [TestCase("<p>text")]
  [TestCase("text<p>text")]
  public void TestToString_WithNoAttributeElements(string input)
  {
    var doc = new HtmlDocument(input);

    Assert.That(doc.Elements.Count, Is.EqualTo(1), "element count");
    Assert.That(doc.Elements[0].LocalName, Is.EqualTo("p"), "element local name #0");

    Assert.That(doc.ToString(), Is.EqualTo(input), "reconstructed document");
  }

  [TestCase("</p>")]
  [TestCase("text</p>")]
  [TestCase("</p>text")]
  [TestCase("text</p>text")]
  public void TestToString_WithElementEnds(string input)
  {
    var doc = new HtmlDocument(input);

    Assert.That(doc.Elements.Count, Is.EqualTo(0), "element count");

    Assert.That(doc.ToString(), Is.EqualTo(input), "reconstructed document");
  }

  [TestCase("<p>text<p>")]
  [TestCase("text<p>text<p>")]
  [TestCase("text<p>text<p>text")]
  [TestCase("<p>text<p>text<p>text")]
  [TestCase("<p>text<p>text<p>text<p>")]
  public void TestToString_WithMultipleElements(string input)
  {
    var doc = new HtmlDocument(input);

    Assert.That(doc.ToString(), Is.EqualTo(input), "reconstructed document");
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

    Assert.That(doc.Elements.Count, Is.EqualTo(1), "element count");
    Assert.That(doc.Elements[0].LocalName, Is.EqualTo("p"), "element local name #0");

    Assert.That(doc.Elements[0].Attributes.Count, Is.EqualTo(2), "element #0 attribute count");

    Assert.That(doc.Elements[0].Attributes[0].Name, Is.EqualTo("class"), "element #0 attribute #0 name");
    Assert.That(doc.Elements[0].Attributes[0].Value, Is.EqualTo("parag"), "element #0 attribute #0 value");

    Assert.That(doc.Elements[0].Attributes[1].Name, Is.EqualTo("id"), "element #0 attribute #1 name");
    Assert.That(doc.Elements[0].Attributes[1].Value, Is.EqualTo("p1"), "element #0 attribute #1 value");

    Assert.That(doc.ToString(), Is.EqualTo(input), "reconstructed document");
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

    Assert.That(doc.Elements.Count, Is.EqualTo(1), "element count");
    Assert.That(doc.Elements[0].LocalName, Is.EqualTo("e"), "element local name #0");

    Assert.That(doc.Elements[0].Attributes.Count, Is.EqualTo(2), "element #0 attribute count");

    Assert.That(doc.Elements[0].Attributes[0].Name, Is.EqualTo("attr1"), "element #0 attribute #0 name");
    Assert.That(doc.Elements[0].Attributes[0].Value, Is.EqualTo("val1"), "element #0 attribute #0 value");

    Assert.That(doc.Elements[0].Attributes[1].Name, Is.EqualTo("attr2"), "element #0 attribute #1 name");
    Assert.That(doc.Elements[0].Attributes[1].Value, Is.EqualTo(null), "element #0 attribute #1 value");

    Assert.That(doc.ToString(), Is.EqualTo(input), "reconstructed document");
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

    Assert.That(doc.Elements.Count, Is.EqualTo(1), "element count");
    Assert.That(doc.Elements[0].LocalName, Is.EqualTo("e"), "element local name #0");

    Assert.That(doc.Elements[0].Attributes.Count, Is.EqualTo(2), "element #0 attribute count");

    Assert.That(doc.Elements[0].Attributes[0].Name, Is.EqualTo("attr1"), "element #0 attribute #0 name");
    Assert.That(doc.Elements[0].Attributes[0].Value, Is.EqualTo(null), "element #0 attribute #0 value");

    Assert.That(doc.Elements[0].Attributes[1].Name, Is.EqualTo("attr2"), "element #0 attribute #1 name");
    Assert.That(doc.Elements[0].Attributes[1].Value, Is.EqualTo("val2"), "element #0 attribute #1 value");

    Assert.That(doc.ToString(), Is.EqualTo(input), "reconstructed document");
  }

  [TestCase("<e attr='val'>", "<e attr='al'>")]
  [TestCase("<e attr='val'/>", "<e attr='al'/>")]
  [TestCase("<e attr='val' />", "<e attr='al' />")]
  [TestCase("<e attr= 'val' />", "<e attr= 'al' />")]
  [TestCase("<e attr=\"val\">", "<e attr=\"al\">")]
  [TestCase("text<e attr='val'>text", "text<e attr='al'>text")]
  [TestCase("text<e attr='val'>text<e attr='val'>", "text<e attr='al'>text<e attr='al'>")]
  [TestCase("text<e attr='val'>text<e attr='val'>text", "text<e attr='al'>text<e attr='al'>text")]
  public void TestToString_ModifyAttributeValue_LengthInflating(string input, string expectedResult)
  {
    var doc = new HtmlDocument(input);

    foreach (var e in doc.Elements) {
      foreach (var a in e.Attributes) {
        a.Value = a.Value!.Substring(1);
      }
    }

    Assert.That(doc.ToString(), Is.EqualTo(expectedResult), "reconstructed document");
  }

  [TestCase("<e attr='val'>", "<e attr=' val'>")]
  [TestCase("<e attr='val'/>", "<e attr=' val'/>")]
  [TestCase("<e attr='val' />", "<e attr=' val' />")]
  [TestCase("<e attr= 'val' />", "<e attr= ' val' />")]
  [TestCase("<e attr=\"val\">", "<e attr=\" val\">")]
  [TestCase("text<e attr='val'>text", "text<e attr=' val'>text")]
  [TestCase("text<e attr='val'>text<e attr='val'>", "text<e attr=' val'>text<e attr=' val'>")]
  [TestCase("text<e attr='val'>text<e attr='val'>text", "text<e attr=' val'>text<e attr=' val'>text")]
  public void TestToString_ModifyAttributeValue_LengthDeflating(string input, string expectedResult)
  {
    var doc = new HtmlDocument(input);

    foreach (var e in doc.Elements) {
      foreach (var a in e.Attributes) {
        a.Value = " " + a.Value;
      }
    }

    Assert.That(doc.ToString(), Is.EqualTo(expectedResult), "reconstructed document");
  }
}
