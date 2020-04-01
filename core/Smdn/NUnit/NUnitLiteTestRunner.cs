using NUnitLite;
using System.Reflection;

public class NUnitLiteTestRunner {
  static int Main(string[] args)
  {
    var typeInfo = typeof(NUnitLiteTestRunner).GetTypeInfo();

    return (new AutoRun(typeInfo.Assembly)).Execute(args);
  }
}
