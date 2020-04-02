はてなブログ用ツール(HatenaBlogTools)
================
**HatenaBlogTools**は、[はてなブログ](https://hatenablog.com/)の記事をエクスポートしたり投稿・編集を行うためのコマンドラインツール集です。　HatenaBlogToolsは[はてなブログAtomPub API](http://developer.hatena.ne.jp/ja/documents/blog/apis/atom)を用いています。

**HatenaBlogTools** is command line editting tools for Hatena Blog.

# License
This software is released under the MIT License, see [LICENSE](/LICENSE).

# Requirements
Requires one of the following .NET runtimes.

- [.NET Core Runtime](https://www.microsoft.com/net/download/all) 3.1 or over
- [Mono](http://www.go-mono.com/mono-downloads/download.html) 6.6 or over
- .NET Framework 4.7 or over

# Install & How to use
see https://smdn.jp/works/tools/HatenaBlogTools/

# How to build and run from source
The instructions of using 'Login' command is shown in below.

```console
# change directory to 'Login' command
cd cli/Login/

# build 'Login' command
dotnet build -f netcoreapp3.1

# run 'Login' command
dotnet run -f netcoreapp3.1 -- --id <your-hatena-id> --blog-id <your.hatena.blog.domain> --api-key <your-api-key>

# show usage of 'Login' command
dotnet run -f netcoreapp3.1 -- --help
```

> 'Login' command only attempts to login Hatena Blog, and does not edit anything even if the login is successful.
