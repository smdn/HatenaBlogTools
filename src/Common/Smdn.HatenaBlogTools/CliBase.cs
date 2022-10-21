// SPDX-FileCopyrightText: 2018 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#nullable enable

using System;
using System.Collections.Generic;
#if NULL_STATE_STATIC_ANALYSIS_ATTRIBUTES
using System.Diagnostics.CodeAnalysis;
#endif
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.Versioning;

using Smdn.HatenaBlogTools.HatenaBlog;

namespace Smdn.HatenaBlogTools;

public class AbortCommandException : Exception { }

public abstract class CliBase {
  internal static class AssemblyInfo {
    private static Assembly? Assembly { get; } = Assembly.GetEntryAssembly();

    public static string? Name => Assembly?.GetName()?.Name;
    public static Version? Version => Assembly?.GetName()?.Version;
    public static string? VersionMajorMinorString => Version is null ? null : $"{Version.Major}.{Version.Minor}";
    public static string? Title => Assembly?.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
    public static string? InformationalVersion => Assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
    public static string? TargetFramework => GetTargetFrameworkNameOrMoniker();

    private static string? GetTargetFrameworkNameOrMoniker()
    {
      var frameworkDisplayName = Assembly?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkDisplayName;

      if (!string.IsNullOrEmpty(frameworkDisplayName))
        return frameworkDisplayName;

      var frameworkName = Assembly?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;

      return FrameworkNameUtils.TryGetMoniker(frameworkName, out var moniker)
        ? moniker
        : frameworkName;
    }
  }

  internal static string UserAgent => $"{AssemblyInfo.Title?.Replace(' ', '-')}/{AssemblyInfo.VersionMajorMinorString} ({AssemblyInfo.TargetFramework ?? ".net"}; {Environment.OSVersion.VersionString})";

  protected bool ParseCommonCommandLineArgs(ref string[] args, out HatenaBlogAtomPubCredential? credential)
  {
    return ParseCommonCommandLineArgs(
      ref args,
      Array.Empty<string>(),
      out credential
    );
  }

  protected bool ParseCommonCommandLineArgs(
    ref string[] args,
    string[] argsNotRequireHatenaBlogClient,
    out HatenaBlogAtomPubCredential? credential
  )
  {
    credential = null;

    var _argsNotRequireHatenaBlogClient = new List<string>() {
      "/?",
      "/help",
      "-h",
      "--help",
      "--version",
    };

    _argsNotRequireHatenaBlogClient.AddRange(argsNotRequireHatenaBlogClient);

    var requireHatenaBlogClient = !new HashSet<string>(args, StringComparer.Ordinal).Overlaps(_argsNotRequireHatenaBlogClient);

    string? hatenaId = null;
    string? blogId = null;
    string? apiKey = null;
    var unparsedArgs = new List<string>(args.Length);

    for (var i = 0; i < args.Length; i++) {
      switch (args[i]) {
        case "--id":
        case "-id":
          hatenaId = args[++i];
          break;

        case "--blog-id":
        case "-blogid":
          blogId = args[++i];
          break;

        case "--api-key":
        case "-apikey":
          apiKey = args[++i];
          break;

        case "--version":
          Version();
          break;

        case "/?":
        case "/help":
        case "-h":
        case "--help":
          Usage(null);
          break;

        default:
          unparsedArgs.Add(args[i]);
          break;
      }
    }

    if (requireHatenaBlogClient) {
      if (string.IsNullOrEmpty(hatenaId)) {
        Usage("--idを指定してください");
        return false;
      }

      if (string.IsNullOrEmpty(blogId)) {
        Usage("--blog-idを指定してください");
        return false;
      }

      if (string.IsNullOrEmpty(apiKey)) {
        Usage("--api-keyを指定してください");
        return false;
      }

      credential = new HatenaBlogAtomPubCredential(hatenaId!, blogId!, apiKey!);
    }

    args = unparsedArgs.ToArray();

    return true;
  }

  protected static HatenaBlogAtomPubClient CreateClient(HatenaBlogAtomPubCredential credential)
  {
    HatenaBlogAtomPubClient.InitializeHttpsServicePoint();

    var client = HatenaBlogAtomPubClient.Create(credential);

    client.UserAgent = UserAgent;

    return client;
  }

  protected static bool Login(
    HatenaBlogAtomPubCredential credential,
#if NULL_STATE_STATIC_ANALYSIS_ATTRIBUTES
    [NotNullWhen(true)]
#endif
    out HatenaBlogAtomPubClient? hatenaBlog
  )
  {
    hatenaBlog = CreateClient(credential);

    Console.Write("ログインしています ... ");

    var statusCode = hatenaBlog.Login(out _);

    if (statusCode == HttpStatusCode.OK) {
      Console.ForegroundColor = ConsoleColor.Green;
      Console.WriteLine("ログインに成功しました。");
      Console.ResetColor();

      return true;
    }
    else {
      hatenaBlog = null;

      Console.ForegroundColor = ConsoleColor.Red;
      Console.Error.WriteLine("ログインに失敗しました。　({0:D} {0})", statusCode);
      Console.ResetColor();

      return false;
    }
  }

  protected abstract string GetDescription();

  protected abstract string GetUsageExtraMandatoryOptions();

  protected abstract IEnumerable<string> GetUsageExtraOptionDescriptions();

  private static void Version()
  {
    Console.WriteLine(AssemblyInfo.InformationalVersion);

    Environment.Exit(0);
  }

  protected void Usage(string? format, params string[] args)
  {
    if (format != null) {
      Console.ForegroundColor = ConsoleColor.Red;
      Console.Error.Write("error: ");
      Console.Error.WriteLine(format, args);
      Console.ResetColor();

      Console.Error.WriteLine();
    }

    var assm = Assembly.GetEntryAssembly();
    var commandLine = Runtime.RuntimeEnvironment switch {
      RuntimeEnvironment.NetCore => $"dotnet {System.IO.Path.GetFileName(assm.Location)} --",
      RuntimeEnvironment.Mono => $"mono {System.IO.Path.GetFileName(assm.Location)}",
      _ => $"{System.IO.Path.GetFileName(assm.Location)}",
    };
    Console.Error.WriteLine($"{AssemblyInfo.Title} version {AssemblyInfo.InformationalVersion}");
    Console.Error.WriteLine($"User-Agent: {UserAgent}");
    Console.Error.WriteLine();
    Console.Error.WriteLine("説明: " + GetDescription());
    Console.Error.WriteLine();
    Console.Error.WriteLine($"使い方: {commandLine} --id <はてなID> --blog-id <ブログID> --api-key <APIキー> " + GetUsageExtraMandatoryOptions());
    Console.Error.WriteLine();
    Console.Error.WriteLine("  --id <はてなID>       : はてなID ( https://blog.hatena.ne.jp/-/config にて確認できます)");
    Console.Error.WriteLine("  --blog-id <ブログID>  : ブログID (xxx.hatenablog.jp, xxx.hateblo.jpなどのブログドメイン)");
    Console.Error.WriteLine("  --api-key <APIキー>   : AtomPub APIキー ( https://blog.hatena.ne.jp/my/config/detail より取得できます)");
    Console.Error.WriteLine();

    Console.Error.WriteLine("オプション:");

    foreach (var extraOptionDescription in GetUsageExtraOptionDescriptions()) {
      Console.Error.WriteLine($"  {extraOptionDescription}");
    }

    Console.Error.WriteLine();
    Console.Error.WriteLine("  --version               : バージョン情報を表示します");
    Console.Error.WriteLine("  -h, --help, /help, /?   : 使い方を表示します");

    throw new AbortCommandException();
  }
}
