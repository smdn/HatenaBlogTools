using System;
using NUnit.Framework;

namespace Smdn.Applications.HatenaBlogTools {
  [TestFixture]
  public class MovableTypeFormatsTests {
    [Test]
    public void TestToDateString()
    {
      //MM/dd/yyyy hh\:mm\:ss tt
      Assert.AreEqual(
        "03/31/2020 12:01:02 AM",
        MovableTypeFormats.ToDateString(new DateTime(2020, 3, 31, 0, 1, 2))
      );
      Assert.AreEqual(
        "03/31/2020 01:01:02 AM",
        MovableTypeFormats.ToDateString(new DateTime(2020, 3, 31, 1, 1, 2))
      );
      Assert.AreEqual(
        "03/31/2020 11:01:02 AM",
        MovableTypeFormats.ToDateString(new DateTime(2020, 3, 31, 11, 1, 2))
      );
      Assert.AreEqual(
        "03/31/2020 12:01:02 PM",
        MovableTypeFormats.ToDateString(new DateTime(2020, 3, 31, 12, 1, 2))
      );
      Assert.AreEqual(
        "03/31/2020 01:01:02 PM",
        MovableTypeFormats.ToDateString(new DateTime(2020, 3, 31, 13, 1, 2))
      );
      Assert.AreEqual(
        "03/31/2020 11:01:02 PM",
        MovableTypeFormats.ToDateString(new DateTime(2020, 3, 31, 23, 1, 2))
      );
    }
  }
}
