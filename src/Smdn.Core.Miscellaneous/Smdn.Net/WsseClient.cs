// SPDX-FileCopyrightText: 2010 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Smdn.Net;

public static class WsseClient {
  private const string HeaderName = "X-WSSE";

  public static string CreateWsseUsernameToken(NetworkCredential credential)
    => CreateWsseUsernameToken(credential, DateTimeOffset.Now);

  public static string CreateWsseUsernameToken(NetworkCredential credential, DateTimeOffset createdDateTime)
  {
    if (credential == null)
      throw new ArgumentNullException(nameof(credential));

    const int nonceLength = 40;
    var nonce = new byte[nonceLength];
    var rng = RandomNumberGenerator.Create();

    rng.GetBytes(nonce);

    var nonceBase64 = Convert.ToBase64String(nonce, Base64FormattingOptions.None);

    var createdDateTimeString = createdDateTime.ToString("o");

    string passwordDigest;

#pragma warning disable CA5350
    using var hash = SHA1.Create();
#pragma warning restore CA5350

    var buffer = new MemoryStream();
    var writer = new BinaryWriter(buffer);

    writer.Write(nonce);
    writer.Write(Encoding.ASCII.GetBytes(createdDateTimeString));
    writer.Write(Encoding.ASCII.GetBytes(credential.Password));
    writer.Flush();

    buffer.Position = 0L;

    passwordDigest = Convert.ToBase64String(
      hash.ComputeHash(buffer),
      Base64FormattingOptions.None
    );

    return $"UsernameToken Username=\"{credential.UserName}\", PasswordDigest=\"{passwordDigest}\", Nonce=\"{nonceBase64}\", Created=\"{createdDateTimeString}\"";
  }

  public static void SetWsseHeader(this WebRequest request, NetworkCredential credential)
    => SetWsseHeader(request, credential, DateTimeOffset.Now);

  public static void SetWsseHeader(this WebRequest request, NetworkCredential credential, DateTimeOffset createdDateTime)
  {
    request.Headers[HeaderName] = CreateWsseUsernameToken(
      credential,
      createdDateTime
    );
  }
}
