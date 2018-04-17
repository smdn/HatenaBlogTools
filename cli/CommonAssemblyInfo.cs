using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyCompany("smdn:総武ソフトウェア推進所")]
[assembly: AssemblyCopyright(AssemblyInfo.Copyright)]
[assembly: AssemblyTitle(AssemblyInfo.SubName)]
[assembly: AssemblyProduct(AssemblyInfo.Name + "-" + AssemblyInfo.Version + " (" + AssemblyInfo.TargetFramework + ")")]
[assembly: AssemblyVersion(AssemblyInfo.Version)]
[assembly: AssemblyInformationalVersion(AssemblyInfo.Version + AssemblyInfo.VersionSuffix)]
[assembly: AssemblyConfiguration(AssemblyInfo.TargetFramework + AssemblyInfo.Configuration)]

internal static partial class AssemblyInfo {
  internal const string Name = "HatenaBlogTools";
  internal const string Version = "1.06";
  internal const string Copyright = "Copyright (C) 2013-2014 smdn";

  internal const string TargetFramework =
#if NET46
    ".NET Framework 4.6"
    #elif NETCOREAPP2_0
    ".NET Core 2.0"
#else
    ""
#endif
    ;

  internal const string TargetFrameworkSuffix =
#if NET46
    "net4.6"
#elif NETCOREAPP2_0
    "netcoreapp2.0"
#else
    ""
#endif
    ;

  internal const string Configuration =
#if DEBUG
    " Debug"
#else
    ""
#endif
    ;
}