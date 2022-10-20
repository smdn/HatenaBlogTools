// SPDX-FileCopyrightText: 2013 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System.Xml.Linq;

namespace Smdn.HatenaBlogTools.AtomPublishingProtocol;

public static class AtomPub {
  public static class Namespaces {
    public static readonly XNamespace Atom = (XNamespace)"http://www.w3.org/2005/Atom";
    public static readonly XNamespace App = (XNamespace)"http://www.w3.org/2007/app";
    public static readonly XNamespace Hatena = (XNamespace)"http://www.hatena.ne.jp/info/xmlns#";
  }

  public static class ElementNames {
    public static readonly XName AppService = Namespaces.App + "service";
    public static readonly XName AppWorkspace = Namespaces.App + "workspace";
    public static readonly XName AppCollection = Namespaces.App + "collection";
    public static readonly XName AppAccept = Namespaces.App + "accept";

    public static readonly XName AtomTitle = Namespaces.Atom + "title";
  }
}
