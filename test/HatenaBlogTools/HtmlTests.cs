using System;
using System.Linq;
using NUnit.Framework;

namespace Smdn.Applications.HatenaBlogTools {
  [TestFixture]
  public class HtmlTests {
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
      var e = Html.EnumerateHtmlElementStart(input).FirstOrDefault();

      Assert.IsNotNull(e);
      Assert.AreEqual(expectedLocalName, e.LocalName, "local name");
      Assert.IsEmpty(e.Attributes, "attributes");
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
      var e = Html.EnumerateHtmlElementStart(input).FirstOrDefault();

      Assert.IsNull(e);
    }

    [TestCase("<e>")]
    [TestCase("<e >")]
    [TestCase("<e  >")]
    [TestCase("<e/>")]
    [TestCase("<e />")]
    [TestCase("<e  />")]
    public void TestRegexElementStart_WhiteSpacesAndSelfClosing_WithNoAttributes(string input)
    {
      var e = Html.EnumerateHtmlElementStart(input).FirstOrDefault();

      Assert.IsNotNull(e);
      Assert.AreEqual("e", e.LocalName, "local name");
      Assert.IsEmpty(e.Attributes, "attributes");
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
      var e = Html.EnumerateHtmlElementStart(input).FirstOrDefault();

      Assert.IsNotNull(e);
      Assert.AreEqual("e", e.LocalName, "local name");
      Assert.AreEqual(1, e.Attributes.Count, "attribute count");
      Assert.AreEqual("attr", e.Attributes[0].Name, "attribute name #0");
      Assert.IsNull(e.Attributes[0].Value, "attribute value #0");
    }

    [TestCase("<e attr=\"\">")]
    [TestCase("<e attr=''>")]
    [TestCase("<e attr=\"\" />")]
    [TestCase("<e attr='' />")]
    public void TestRegexElementStart_WhiteSpacesAndSelfClosing_WithQuotedEmptyAttributes(string input)
    {
      var e = Html.EnumerateHtmlElementStart(input).FirstOrDefault();

      Assert.IsNotNull(e);
      Assert.AreEqual("e", e.LocalName, "local name");
      Assert.AreEqual(1, e.Attributes.Count, "attribute count");
      Assert.AreEqual("attr", e.Attributes[0].Name, "attribute name #0");
      Assert.IsEmpty(e.Attributes[0].Value, "attribute value #0");
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
      var e = Html.EnumerateHtmlElementStart(input).FirstOrDefault();

      Assert.IsNotNull(e);
      Assert.AreEqual("e", e.LocalName, "local name");
      Assert.AreEqual(1, e.Attributes.Count, "attribute count");
      Assert.AreEqual("attr", e.Attributes[0].Name, "attribute name #0");
      Assert.AreEqual("val", e.Attributes[0].Value, "attribute value #0");
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
      var e = Html.EnumerateHtmlElementStart(input).FirstOrDefault();

      Assert.IsNotNull(e);
      Assert.AreEqual("e", e.LocalName, "local name");
      Assert.AreEqual(1, e.Attributes.Count, "attribute count");
      Assert.AreEqual("attr", e.Attributes[0].Name, "attribute name #0");
      Assert.AreEqual("val", e.Attributes[0].Value, "attribute value #0");
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
      var e = Html.EnumerateHtmlElementStart(input).FirstOrDefault();

      Assert.IsNotNull(e);
      Assert.AreEqual("e", e.LocalName, "local name");
      Assert.AreEqual(1, e.Attributes.Count, "attribute count");
      Assert.AreEqual("attr", e.Attributes[0].Name, "attribute name #0");
      Assert.AreEqual("val", e.Attributes[0].Value, "attribute value #0");
    }

    [TestCase("<e attr=\"foo=bar\">")]
    [TestCase("<e attr='foo=bar'>")]
    public void TestRegexElementStart_QuotedAttributeValueWithSpecialChars_1(string input)
    {
      var e = Html.EnumerateHtmlElementStart(input).FirstOrDefault();

      Assert.IsNotNull(e);
      Assert.AreEqual("e", e.LocalName, "local name");
      Assert.AreEqual(1, e.Attributes.Count, "attribute count");
      Assert.AreEqual("attr", e.Attributes[0].Name, "attribute name #0");
      Assert.AreEqual("foo=bar", e.Attributes[0].Value, "attribute value #0");
    }

    [TestCase("<e attr=\"<foo>\">")]
    [TestCase("<e attr='<foo>'>")]
    public void TestRegexElementStart_QuotedAttributeValueWithSpecialChars_2(string input)
    {
      var e = Html.EnumerateHtmlElementStart(input).FirstOrDefault();

      Assert.IsNotNull(e);
      Assert.AreEqual("e", e.LocalName, "local name");
      Assert.AreEqual(1, e.Attributes.Count, "attribute count");
      Assert.AreEqual("attr", e.Attributes[0].Name, "attribute name #0");
      Assert.AreEqual("<foo>", e.Attributes[0].Value, "attribute value #0");
    }

    [TestCase("<e attr1 attr2>")]
    [TestCase("<e attr1 attr2 />")]
    [TestCase("<e  attr1 attr2 />")]
    [TestCase("<e  attr1  attr2 />")]
    [TestCase("<e  attr1  attr2  />")]
    public void TestRegexElementStart_MultipleAttributes_EmptyAttribute(string input)
    {
      var e = Html.EnumerateHtmlElementStart(input).FirstOrDefault();

      Assert.IsNotNull(e);
      Assert.AreEqual("e", e.LocalName, "local name");

      Assert.AreEqual(2, e.Attributes.Count, "attribute count");

      Assert.AreEqual("attr1", e.Attributes[0].Name, "attribute name #0");
      Assert.IsNull(e.Attributes[0].Value, "attribute value #0");

      Assert.AreEqual("attr2", e.Attributes[1].Name, "attribute name #1");
      Assert.IsNull(e.Attributes[1].Value, "attribute value #1");
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
      var e = Html.EnumerateHtmlElementStart(input).FirstOrDefault();

      Assert.IsNotNull(e);
      Assert.AreEqual("e", e.LocalName, "local name");

      Assert.AreEqual(2, e.Attributes.Count, "attribute count");

      Assert.AreEqual("attr1", e.Attributes[0].Name, "attribute name #0");
      Assert.AreEqual("val1", e.Attributes[0].Value, "attribute value #0");

      Assert.AreEqual("attr2", e.Attributes[1].Name, "attribute name #1");
      Assert.AreEqual("val2", e.Attributes[1].Value, "attribute name #1");
    }

    [TestCase("<p><br></p>")]
    [TestCase("<p><br> </p>")]
    [TestCase("<p> <br></p>")]
    [TestCase("<p> <br> </p>")]
    public void TestRegexElementStart_StructuredHtml_WithNoAttribute(string input)
    {
      var elements = Html.EnumerateHtmlElementStart(input).ToList();

      Assert.IsNotEmpty(elements);
      Assert.AreEqual(2, elements.Count);

      Assert.AreEqual("p", elements[0].LocalName, "local name #0");
      Assert.IsEmpty(elements[0].Attributes, "attributes #0");

      Assert.AreEqual("br", elements[1].LocalName, "local name #1");
      Assert.IsEmpty(elements[1].Attributes, "attributes #1");
    }

    [TestCase("<p class='para' id='p1'><br></p>")]
    public void TestRegexElementStart_StructuredHtml_WithAttribute(string input)
    {
      var elements = Html.EnumerateHtmlElementStart(input).ToList();

      Assert.IsNotEmpty(elements);
      Assert.AreEqual(2, elements.Count);

      Assert.AreEqual("p", elements[0].LocalName, "local name #0");
      Assert.AreEqual(2, elements[0].Attributes.Count(), "attribute count #0");

      Assert.AreEqual("class", elements[0].Attributes[0].Name, "attribute name #0");
      Assert.AreEqual("para", elements[0].Attributes[0].Value, "attribute value #0");

      Assert.AreEqual("id", elements[0].Attributes[1].Name, "attribute name #1");
      Assert.AreEqual("p1", elements[0].Attributes[1].Value, "attribute value #1");

      Assert.AreEqual("br", elements[1].LocalName, "local name #1");
      Assert.IsEmpty(elements[1].Attributes, "attributes #1");
    }

    [TestCase("<p><i><b>")]
    [TestCase(" <p> <i> <b> ")]
    [TestCase("\n<p>\n<i>\n<b>\n")]
    public void TestRegexElementStart_StructuredHtml_MultipleElement(string input)
    {
      var elements = Html.EnumerateHtmlElementStart(input).ToList();

      Assert.IsNotEmpty(elements);
      Assert.AreEqual(3, elements.Count);

      Assert.AreEqual("p", elements[0].LocalName, "local name #0");
      Assert.IsEmpty(elements[0].Attributes, "attributes #0");

      Assert.AreEqual("i", elements[1].LocalName, "local name #1");
      Assert.IsEmpty(elements[1].Attributes, "attributes #1");

      Assert.AreEqual("b", elements[2].LocalName, "local name #2");
      Assert.IsEmpty(elements[2].Attributes, "attributes #2");
    }
  }
}