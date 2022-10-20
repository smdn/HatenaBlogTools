// SPDX-FileCopyrightText: 2013 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;

namespace Smdn.HatenaBlogTools.HatenaBlog;

public abstract class HatenaBlogAtomPubClient {
  public static void InitializeHttpsServicePoint()
  {
    ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault;

    ServicePointManager.ServerCertificateValidationCallback = (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => {
      if (sslPolicyErrors == SslPolicyErrors.None) {
        return true;
      }
      else {
        Console.Error.WriteLine(sslPolicyErrors);

        return false;
      }
    };
  }

  public static HatenaBlogAtomPubClient Create(HatenaBlogAtomPubCredential credential)
  {
    return new DefaultHatenaBlogAtomPubClient(credential);
  }

#if DEBUG
  public static HatenaBlogAtomPubClient Create(IReadOnlyList<PostedEntry> entries)
  {
    return new PseudoHatenaBlogAtomPubClient(entries);
  }
#endif

  public static IEnumerable<PostedEntry> ReadEntriesFrom(XDocument document)
  {
    foreach (var entry in DefaultHatenaBlogAtomPubClient.ReadEntries(document)) {
      yield return entry.Item1;
    }
  }

  /*
   * instance members
   */
  public abstract string HatenaId { get; }
  public abstract string BlogId { get; }
  public abstract Uri RootEndPoint { get; }
  public abstract string BlogTitle { get; }
  public abstract Uri CollectionUri { get; }
  public abstract string UserAgent { get; set; }

  public abstract void WaitForCinnamon();

  public abstract HttpStatusCode Login(out XDocument serviceDocument);

  public IEnumerable<PostedEntry> EnumerateEntries()
  {
    foreach (var pair in EnumerateAllEntries()) {
      yield return pair.Item1;
    }
  }

  public void EnumerateEntries(Action<PostedEntry, XElement> actionForEachEntry)
  {
    if (actionForEachEntry == null)
      throw new ArgumentNullException(nameof(actionForEachEntry));

    foreach (var pair in EnumerateAllEntries()) {
      actionForEachEntry(pair.Item1, pair.Item2);
    }
  }

  protected abstract IEnumerable<Tuple<PostedEntry, XElement>> EnumerateAllEntries();

  public abstract HttpStatusCode UpdateEntry(PostedEntry updatingEntry, out XDocument responseDocument);

  public abstract HttpStatusCode PostEntry(Entry entry, out XDocument responseDocument);
}
