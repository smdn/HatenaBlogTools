using System;
using System.Linq;
using NUnit.Framework;

namespace Smdn.HatenaBlogTools.Html;

[TestFixture]
public class HtmlParserTests {
  [TestCase("<p>", "p")]
  [TestCase("<p> ", "p")]
  [TestCase(" <p>", "p")]
  [TestCase(" <p> ", "p")]
  [TestCase("<p/>", "p")]
  [TestCase("<p></p>", "p")]
  [TestCase("<p> </p>", "p")]
  [TestCase("<P>", "P")]
  [TestCase("<P/>", "P")]
  [TestCase("<br>", "br")]
  [TestCase("<br/>", "br")]
  [TestCase("<source>", "source")]
  [TestCase("<source/>", "source")]
  public void TestRegexElementStart_Elements(string input, string expectedLocalName)
  {
    var e = HtmlParser.EnumerateHtmlElementStart(input).FirstOrDefault();

    Assert.That(e, Is.Not.Null);
    Assert.That(e!.LocalName, Is.EqualTo(expectedLocalName), "local name");
    Assert.That(e.Attributes, Is.Empty, "attributes");
  }

  [TestCase("</p>")]
  [TestCase("</p> ")]
  [TestCase(" </p>")]
  [TestCase(" </p> ")]
  [TestCase("</P>")]
  [TestCase("</BR>")]
  [TestCase("</source>")]
  public void TestRegexElementStart_Elements_MustNotMatchElementEnd(string input)
  {
    var e = HtmlParser.EnumerateHtmlElementStart(input).FirstOrDefault();

    Assert.That(e, Is.Null);
  }

  [TestCase("<e>")]
  [TestCase("<e >")]
  [TestCase("<e  >")]
  [TestCase("<e/>")]
  [TestCase("<e />")]
  [TestCase("<e  />")]
  public void TestRegexElementStart_WhiteSpacesAndSelfClosing_WithNoAttributes(string input)
  {
    var e = HtmlParser.EnumerateHtmlElementStart(input).FirstOrDefault();

    Assert.That(e, Is.Not.Null);
    Assert.That(e!.LocalName, Is.EqualTo("e"), "local name");
    Assert.That(e.Attributes, Is.Empty, "attributes");
  }

  [TestCase("<e attr>")]
  [TestCase("<e attr >")]
  [TestCase("<e attr  >")]
  [TestCase("<e  attr>")]
  [TestCase("<e  attr >")]
  [TestCase("<e  attr  >")]
  [TestCase("<e attr/>")]
  [TestCase("<e attr />")]
  [TestCase("<e attr  />")]
  [TestCase("<e  attr/>")]
  [TestCase("<e  attr />")]
  [TestCase("<e  attr  />")]
  public void TestRegexElementStart_WhiteSpacesAndSelfClosing_WithEmptyAttributes(string input)
  {
    var e = HtmlParser.EnumerateHtmlElementStart(input).FirstOrDefault();

    Assert.That(e, Is.Not.Null);
    Assert.That(e!.LocalName, Is.EqualTo("e"), "local name");
    Assert.That(e.Attributes.Count, Is.EqualTo(1), "attribute count");
    Assert.That(e.Attributes[0].Name, Is.EqualTo("attr"), "attribute name #0");
    Assert.That(e.Attributes[0].Value, Is.Null, "attribute value #0");
  }

  [TestCase("<e attr=\"\">")]
  [TestCase("<e attr=''>")]
  [TestCase("<e attr=\"\" />")]
  [TestCase("<e attr='' />")]
  public void TestRegexElementStart_WhiteSpacesAndSelfClosing_WithQuotedEmptyAttributes(string input)
  {
    var e = HtmlParser.EnumerateHtmlElementStart(input).FirstOrDefault();

    Assert.That(e, Is.Not.Null);
    Assert.That(e!.LocalName, Is.EqualTo("e"), "local name");
    Assert.That(e!.Attributes.Count, Is.EqualTo(1), "attribute count");
    Assert.That(e!.Attributes[0].Name, Is.EqualTo("attr"), "attribute name #0");
    Assert.That(e!.Attributes[0].Value, Is.Empty, "attribute value #0");
  }

  [TestCase("<e attr=val>")]
  [TestCase("<e attr=val >")]
  [TestCase("<e attr=val  >")]
  [TestCase("<e  attr=val>")]
  [TestCase("<e  attr=val >")]
  [TestCase("<e  attr=val  >")]
  //[TestCase("<e attr=val/>")] // invalid: must be separated with space ' '
  [TestCase("<e attr=val />")]
  [TestCase("<e attr=val  />")]
  //[TestCase("<e  attr=val/>")] // invalid: must be separated with space ' '
  [TestCase("<e  attr=val />")]
  [TestCase("<e  attr=val  />")]
  public void TestRegexElementStart_WhiteSpacesAndSelfClosing_WithUnquotedValueAttributes(string input)
  {
    var e = HtmlParser.EnumerateHtmlElementStart(input).FirstOrDefault();

    Assert.That(e, Is.Not.Null);
    Assert.That(e!.LocalName, Is.EqualTo("e"), "local name");
    Assert.That(e!.Attributes.Count, Is.EqualTo(1), "attribute count");
    Assert.That(e!.Attributes[0].Name, Is.EqualTo("attr"), "attribute name #0");
    Assert.That(e!.Attributes[0].Value, Is.EqualTo("val"), "attribute value #0");
  }

  [TestCase("<e attr=val>")]
  [TestCase("<e attr =val>")]
  [TestCase("<e attr  =val>")]
  [TestCase("<e attr= val>")]
  [TestCase("<e attr=  val>")]
  [TestCase("<e attr=val >")]
  [TestCase("<e attr=val  >")]
  [TestCase("<e attr= val >")]
  [TestCase("<e attr=  val  >")]

  [TestCase("<e attr=\"val\">")]
  [TestCase("<e attr =\"val\">")]
  [TestCase("<e attr  =\"val\">")]
  [TestCase("<e attr= \"val\">")]
  [TestCase("<e attr=  \"val\">")]
  [TestCase("<e attr=\"val\" >")]
  [TestCase("<e attr=\"val\"  >")]
  [TestCase("<e attr= \"val\" >")]
  [TestCase("<e attr=  \"val\"  >")]

  [TestCase("<e attr='val'>")]
  [TestCase("<e attr= 'val'>")]
  [TestCase("<e attr=  'val'>")]
  [TestCase("<e attr ='val'>")]
  [TestCase("<e attr  ='val'>")]
  [TestCase("<e attr='val' >")]
  [TestCase("<e attr='val'  >")]
  [TestCase("<e attr= 'val' >")]
  [TestCase("<e attr=  'val'  >")]
  public void TestRegexElementStart_AttributeAndWhiteSpaces(string input)
  {
    var e = HtmlParser.EnumerateHtmlElementStart(input).FirstOrDefault();

    Assert.That(e, Is.Not.Null);
    Assert.That(e!.LocalName, Is.EqualTo("e"), "local name");
    Assert.That(e!.Attributes.Count, Is.EqualTo(1), "attribute count");
    Assert.That(e!.Attributes[0].Name, Is.EqualTo("attr"), "attribute name #0");
    Assert.That(e!.Attributes[0].Value, Is.EqualTo("val"), "attribute value #0");
  }

  [TestCase("<e\tattr=val>")]
  [TestCase("<e\nattr=val>")]
  [TestCase("<e\rattr=val>")]
  [TestCase("<e\fattr=val>")]
  [TestCase("<e\tattr=val\t>")]
  [TestCase("<e\tattr=val\n>")]
  [TestCase("<e\tattr=val\r>")]
  [TestCase("<e\tattr=val\f>")]
  [TestCase("<e\nattr\t=val>")]
  [TestCase("<e\nattr\n=val>")]
  [TestCase("<e\nattr\r=val>")]
  [TestCase("<e\nattr\f=val>")]
  [TestCase("<e\rattr=\tval>")]
  [TestCase("<e\rattr=\nval>")]
  [TestCase("<e\rattr=\rval>")]
  [TestCase("<e\rattr=\fval>")]
  [TestCase("<e\fattr\t=\tval>")]
  [TestCase("<e\fattr\t=\nval>")]
  [TestCase("<e\fattr\t=\rval>")]
  [TestCase("<e\fattr\t=\fval>")]
  public void TestRegexElementStart_AttributeAndWhiteSpaces_NonSPWhiteSpaces(string input)
  {
    var e = HtmlParser.EnumerateHtmlElementStart(input).FirstOrDefault();

    Assert.That(e, Is.Not.Null);
    Assert.That(e!.LocalName, Is.EqualTo("e"), "local name");
    Assert.That(e!.Attributes.Count, Is.EqualTo(1), "attribute count");
    Assert.That(e!.Attributes[0].Name, Is.EqualTo("attr"), "attribute name #0");
    Assert.That(e!.Attributes[0].Value, Is.EqualTo("val"), "attribute value #0");
  }

  [TestCase("<e attr=\"foo=bar\">")]
  [TestCase("<e attr='foo=bar'>")]
  public void TestRegexElementStart_QuotedAttributeValueWithSpecialChars_1(string input)
  {
    var e = HtmlParser.EnumerateHtmlElementStart(input).FirstOrDefault();

    Assert.That(e, Is.Not.Null);
    Assert.That(e!.LocalName, Is.EqualTo("e"), "local name");
    Assert.That(e!.Attributes.Count, Is.EqualTo(1), "attribute count");
    Assert.That(e!.Attributes[0].Name, Is.EqualTo("attr"), "attribute name #0");
    Assert.That(e!.Attributes[0].Value, Is.EqualTo("foo=bar"), "attribute value #0");
  }

  [TestCase("<e attr=\"<foo>\">")]
  [TestCase("<e attr='<foo>'>")]
  public void TestRegexElementStart_QuotedAttributeValueWithSpecialChars_2(string input)
  {
    var e = HtmlParser.EnumerateHtmlElementStart(input).FirstOrDefault();

    Assert.That(e, Is.Not.Null);
    Assert.That(e!.LocalName, Is.EqualTo("e"), "local name");
    Assert.That(e!.Attributes.Count, Is.EqualTo(1), "attribute count");
    Assert.That(e!.Attributes[0].Name, Is.EqualTo("attr"), "attribute name #0");
    Assert.That(e!.Attributes[0].Value, Is.EqualTo("<foo>"), "attribute value #0");
  }

  [TestCase("<e attr1 attr2>")]
  [TestCase("<e attr1 attr2 />")]
  [TestCase("<e  attr1 attr2 />")]
  [TestCase("<e  attr1  attr2 />")]
  [TestCase("<e  attr1  attr2  />")]
  public void TestRegexElementStart_MultipleAttributes_EmptyAttribute(string input)
  {
    var e = HtmlParser.EnumerateHtmlElementStart(input).FirstOrDefault();

    Assert.That(e, Is.Not.Null);
    Assert.That(e!.LocalName, Is.EqualTo("e"), "local name");

    Assert.That(e!.Attributes.Count, Is.EqualTo(2), "attribute count");

    Assert.That(e!.Attributes[0].Name, Is.EqualTo("attr1"), "attribute name #0");
    Assert.That(e!.Attributes[0]!.Value, Is.Null, "attribute value #0");

    Assert.That(e!.Attributes[1].Name, Is.EqualTo("attr2"), "attribute name #1");
    Assert.That(e!.Attributes[1]!.Value, Is.Null, "attribute value #1");
  }

  [TestCase("<e attr1=val1 attr2=val2>")]
  [TestCase("<e attr1=val1 attr2=val2 />")]
  [TestCase("<e attr1='val1' attr2=\"val2\">")]
  [TestCase("<e  attr1=val1 attr2=val2 />")]
  [TestCase("<e  attr1=val1  attr2=val2 />")]
  [TestCase("<e  attr1=val1  attr2=val2  />")]
  [TestCase("<e  attr1=\"val1\"  attr2='val2'  />")]
  public void TestRegexElementStart_MultipleAttributes_WithValue(string input)
  {
    var e = HtmlParser.EnumerateHtmlElementStart(input).FirstOrDefault();

    Assert.That(e, Is.Not.Null);
    Assert.That(e!.LocalName, Is.EqualTo("e"), "local name");

    Assert.That(e!.Attributes.Count, Is.EqualTo(2), "attribute count");

    Assert.That(e.Attributes[0].Name, Is.EqualTo("attr1"), "attribute name #0");
    Assert.That(e.Attributes[0].Value, Is.EqualTo("val1"), "attribute value #0");

    Assert.That(e.Attributes[1].Name, Is.EqualTo("attr2"), "attribute name #1");
    Assert.That(e.Attributes[1].Value, Is.EqualTo("val2"), "attribute name #1");
  }

  [TestCase("<p><br></p>")]
  [TestCase("<p><br> </p>")]
  [TestCase("<p> <br></p>")]
  [TestCase("<p> <br> </p>")]
  public void TestRegexElementStart_StructuredHtml_WithNoAttribute(string input)
  {
    var elements = HtmlParser.EnumerateHtmlElementStart(input).ToList();

    Assert.That(elements, Is.Not.Empty);
    Assert.That(elements.Count, Is.EqualTo(2));

    Assert.That(elements[0].LocalName, Is.EqualTo("p"), "local name #0");
    Assert.That(elements[0].Attributes, Is.Empty, "attributes #0");

    Assert.That(elements[1].LocalName, Is.EqualTo("br"), "local name #1");
    Assert.That(elements[1].Attributes, Is.Empty, "attributes #1");
  }

  [TestCase("<p class='para' id='p1'><br></p>")]
  public void TestRegexElementStart_StructuredHtml_WithAttribute(string input)
  {
    var elements = HtmlParser.EnumerateHtmlElementStart(input).ToList();

    Assert.That(elements, Is.Not.Empty);
    Assert.That(elements.Count, Is.EqualTo(2));

    Assert.That(elements[0].LocalName, Is.EqualTo("p"), "local name #0");
    Assert.That(elements[0].Attributes.Count(), Is.EqualTo(2), "attribute count #0");

    Assert.That(elements[0].Attributes[0].Name, Is.EqualTo("class"), "attribute name #0");
    Assert.That(elements[0].Attributes[0].Value, Is.EqualTo("para"), "attribute value #0");

    Assert.That(elements[0].Attributes[1].Name, Is.EqualTo("id"), "attribute name #1");
    Assert.That(elements[0].Attributes[1].Value, Is.EqualTo("p1"), "attribute value #1");

    Assert.That(elements[1].LocalName, Is.EqualTo("br"), "local name #1");
    Assert.That(elements[1].Attributes, Is.Empty, "attributes #1");
  }

  [TestCase("<p><i><b>")]
  [TestCase(" <p> <i> <b> ")]
  [TestCase("\n<p>\n<i>\n<b>\n")]
  public void TestRegexElementStart_StructuredHtml_MultipleElement(string input)
  {
    var elements = HtmlParser.EnumerateHtmlElementStart(input).ToList();

    Assert.That(elements, Is.Not.Empty);
    Assert.That(elements.Count, Is.EqualTo(3));

    Assert.That(elements[0].LocalName, Is.EqualTo("p"), "local name #0");
    Assert.That(elements[0].Attributes, Is.Empty, "attributes #0");

    Assert.That(elements[1].LocalName, Is.EqualTo("i"), "local name #1");
    Assert.That(elements[1].Attributes, Is.Empty, "attributes #1");

    Assert.That(elements[2].LocalName, Is.EqualTo("b"), "local name #2");
    Assert.That(elements[2].Attributes, Is.Empty, "attributes #2");
  }
}
