//
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
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

using Smdn.Net;

namespace Smdn.Applications.HatenaBlogTools {
  public class Atom {
    public static readonly int DefaultTimeoutMilliseconds = 60 * 1000;

    public int Timeout {
      get { return timeout; }
      set {
        if (value < -1)
          throw new ArgumentOutOfRangeException("Timeout", value, "must be greater than or equals to -1");

        timeout = value;
      }
    }

    public NetworkCredential Credential {
      get { return credential; }
      set { credential = value; }
    }

    private int timeout = DefaultTimeoutMilliseconds;
    private NetworkCredential credential;

    public XmlDocument Get(Uri requestUri, out HttpStatusCode statusCode)
    {
      var req = CreateRequest(WebRequestMethods.Http.Get, requestUri);

      return GetResponse(req, out statusCode);
    }

    public XmlDocument Post(Uri requestUri, XmlDocument document, out HttpStatusCode statusCode)
    {
      return PostPut(WebRequestMethods.Http.Post, requestUri, document, out statusCode);
    }

    public XmlDocument Put(Uri requestUri, XmlDocument document, out HttpStatusCode statusCode)
    {
      return PostPut(WebRequestMethods.Http.Put, requestUri, document, out statusCode);
    }

    private XmlDocument PostPut(string method, Uri requestUri, XmlDocument document, out HttpStatusCode statusCode)
    {
      var req = CreateRequest(method, requestUri);

      req.ContentType = "application/atom+xml";

      using (var reqStream = req.GetRequestStream()) {
        document.Save(reqStream);
      }

      return GetResponse(req, out statusCode);
    }

    private HttpWebRequest CreateRequest(string method, Uri requestUri)
    {
      var req = WebRequest.Create(requestUri) as HttpWebRequest;

      req.Method = method;
      req.SetWsseHeader(credential);
      req.Accept = "application/x.atom+xml, application/atom+xml, application/atomsvc+xml, application/xml, text/xml, */*";
      req.Timeout = timeout;
      req.ReadWriteTimeout = timeout;

      return req;
    }

    private static XmlDocument GetResponse(HttpWebRequest req, out HttpStatusCode statusCode)
    {
      statusCode = (HttpStatusCode)0;

      try {
        using (var resp = req.GetResponse() as HttpWebResponse) {
          statusCode = resp.StatusCode;

          using (var respStream = resp.GetResponseStream()) {
            var doc = new XmlDocument();

            doc.Load(respStream);

            return doc;
          }
        }
      }
      catch (WebException ex) {
        if (ex.Status == WebExceptionStatus.ProtocolError) {
          var resp = ex.Response as HttpWebResponse;

          statusCode = resp.StatusCode;

          Console.Error.WriteLine("{0} {1} ({2} {3})",
                                  (int)resp.StatusCode,
                                  resp.StatusDescription,
                                  req.Method,
                                  resp.ResponseUri);

          using (var respStream = resp.GetResponseStream()) {
            var reader = new StreamReader(respStream, Encoding.UTF8);

            Console.Error.WriteLine(reader.ReadToEnd());
          }

          return null;
        }
        else {
          throw;
        }
      }
    }
  }
}
