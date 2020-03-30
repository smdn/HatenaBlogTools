using System;

namespace Smdn.Applications.HatenaBlogTools {
  public static class MovableTypeFormats {
    /*
     * http://www.movabletype.jp/documentation/appendices/import-export-format.html
     */
    public static string ToDateString(DateTime dateTime)
      => dateTime.ToString("MM/dd/yyyy hh\\:mm\\:ss tt", System.Globalization.CultureInfo.InvariantCulture);
  }
}
