はてなブログ用ツール(HatenaBlogTools)
================
**HatenaBlogTools**は、[はてなブログ](https://hatenablog.com/)の記事をエクスポートしたり投稿・編集を行うためのコマンドラインツール集です。　HatenaBlogToolsは[はてなブログAtomPub API](http://developer.hatena.ne.jp/ja/documents/blog/apis/atom)を用いています。

**HatenaBlogTools** is the command line tools for authoring and editting [Hatena Blog](https://hatenablog.com/).

# License
This software is released under the MIT License, see [LICENSE.txt](/LICENSE.txt).

# Requirements
Requires one of the following **.NET runtimes** to run binaries.

- [.NET Runtime](https://dotnet.microsoft.com/download/dotnet/6.0) 6.0 or over
- [.NET Core Runtime](https://dotnet.microsoft.com/download/dotnet/3.1) 3.1 or over

Also requires **.NET SDK** 6.0 to build and run from source codes.

# Install & How to use
see https://smdn.jp/works/tools/HatenaBlogTools/

# How to build and run from source
The instructions of using 'Login' command is shown in below.

```sh
# change directory to 'Login' command
cd src/Smdn.HatenaBlogTools.Cli.Login/

# restore the build and library dependencies (This is an essential step for a first time build)
dotnet restore

# build the command
dotnet build -f net6.0

# run the command
dotnet run -f net6.0 -- --id <your-hatena-id> --blog-id <your.hatena.blog.domain> --api-key <your-api-key>

# show usage of the command
dotnet run -f net6.0 -- --help
```

> 'Login' command only attempts to login Hatena Blog, and does not edit anything even if the login is successful.
