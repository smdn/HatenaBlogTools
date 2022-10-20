// Author:
//       smdn <smdn@smdn.jp>
//
// Copyright (c) 2013 smdn
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
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
