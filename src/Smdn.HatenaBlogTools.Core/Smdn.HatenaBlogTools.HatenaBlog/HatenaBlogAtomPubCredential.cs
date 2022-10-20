// SPDX-FileCopyrightText: 2013 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;

namespace Smdn.HatenaBlogTools.HatenaBlog;

public class HatenaBlogAtomPubCredential {
  public string HatenaId { get; }
  public string BlogId { get; }
  public string ApiKey { get; }

  public HatenaBlogAtomPubCredential(string hatenaId, string blogId, string apiKey)
  {
    if (string.IsNullOrEmpty(hatenaId))
      throw new ArgumentException("must be non-empty string", nameof(hatenaId));
    if (string.IsNullOrEmpty(blogId))
      throw new ArgumentException("must be non-empty string", nameof(blogId));
    if (string.IsNullOrEmpty(apiKey))
      throw new ArgumentException("must be non-empty string", nameof(apiKey));

    HatenaId = hatenaId;
    BlogId = blogId;
    ApiKey = apiKey;
  }
}
