using System;
using System.Runtime.InteropServices;

namespace Smdn.HatenaBlogTools {
  public static class TimeZoneName {
    private static readonly Version runtimeVersionNET5 = new(5, 0);

    public static bool CanUseIanaTimeZoneName =>
      !Runtime.IsRunningOnNetFx && // .NET Framework uses Windows time zone name
      !(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Runtime.IsRunningOnNetCore && Runtime.Version < runtimeVersionNET5); // Windows + .NET Core uses Windows time zone name
  }
}
