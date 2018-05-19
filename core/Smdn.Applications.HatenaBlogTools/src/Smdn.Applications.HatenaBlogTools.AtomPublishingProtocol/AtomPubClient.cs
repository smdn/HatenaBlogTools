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
using System.Xml.Linq;

using Smdn.Net;

namespace Smdn.Applications.HatenaBlogTools.AtomPublishingProtocol {
  public class AtomPubClient {
    public static readonly int DefaultTimeoutMilliseconds = 20 * 1000;

    private int timeout = DefaultTimeoutMilliseconds;

    public int Timeout {
      get { return timeout; }
      set {
        if (value < -1)
          throw new ArgumentOutOfRangeException("Timeout", value, "must be greater than or equals to -1");

        timeout = value;
      }
    }

    public NetworkCredential Credential {
      get; set;
    }

    public string UserAgent {
      get; set;
    }

    public HttpStatusCode Get(Uri requestUri, out XDocument responseDocument)
    {
      return GetResponse(() => CreateRequest(WebRequestMethods.Http.Get, requestUri), out responseDocument);
    }

    public HttpStatusCode Post(Uri requestUri, XDocument requestDocument, out XDocument responseDocument)
    {
      return PostPut(WebRequestMethods.Http.Post, requestUri, requestDocument, out responseDocument);
    }

    public HttpStatusCode Put(Uri requestUri, XDocument requestDocument, out XDocument responseDocument)
    {
      return PostPut(WebRequestMethods.Http.Put, requestUri, requestDocument, out responseDocument);
    }

    private HttpStatusCode PostPut(string method, Uri requestUri, XDocument requestDocument, out XDocument responseDocument)
    {
      return GetResponse(() => {
        var req = CreateRequest(method, requestUri);

        req.ContentType = "application/atom+xml";

        using (var reqStream = req.GetRequestStream()) {
          requestDocument.Save(reqStream);
        }

        return req;
      }, out responseDocument);
    }

    private HttpWebRequest CreateRequest(string method, Uri requestUri)
    {
      var req = WebRequest.CreateHttp(requestUri);

      req.Method = method;
      req.SetWsseHeader(Credential);
      req.Accept = "application/x.atom+xml, application/atom+xml, application/atomsvc+xml, application/xml, text/xml, */*";
      req.Timeout = timeout;
      req.ReadWriteTimeout = timeout;
      req.UserAgent = UserAgent;

      return req;
    }

    private static readonly int maxTimeoutRetryCount = 3;

    private static HttpStatusCode GetResponse(Func<HttpWebRequest> createRequest, out XDocument responseDocument)
    {
      responseDocument = null;

      for (var timeoutRetryCount = maxTimeoutRetryCount;;) {
        try {
          using (var response = GetResponseCore(createRequest())) {
            if (2 == ((int)response.StatusCode) / 100) { // 2XX
              using (var responseStream = response.GetResponseStream()) {
                responseDocument = XDocument.Load(responseStream);
              }
            }
            else {
              Console.Error.WriteLine("{0} {1} ({2} {3})",
                                      (int)response.StatusCode,
                                      response.StatusDescription,
                                      response.Method,
                                      response.ResponseUri);

#if false
              foreach (string h in resp.Headers.Keys) {
                Console.Error.WriteLine("{0}: {1}", h, resp.Headers[h]);
              }
#endif
              // try read response body
              // XXX: cannot read chunked response with GetResponseStream() (?)
              // XXX: or cannot read empty response (?)
              try {
                using (var memoryStream = new MemoryStream()) {
                  using (var respStream = response.GetResponseStream()) {
                    respStream.CopyTo(memoryStream);
                  }

                  memoryStream.Position = 0L;

                  using (var reader = new StreamReader(memoryStream, Encoding.UTF8)) {
                    Console.Error.WriteLine(reader.ReadToEnd());
                  }
                }
              }
              catch {
                // ignore exceptions
              }
            }

            return response.StatusCode;
          } // using response
        }
        catch (TimeoutException) {
          if (0 < timeoutRetryCount--)
            continue;
          else
            throw;
        }
      }

      HttpWebResponse GetResponseCore(HttpWebRequest req)
      {
        try {
          return req.GetResponse() as HttpWebResponse;
        }
        catch (WebException ex) when (ex.Status == WebExceptionStatus.Timeout) {
          throw new TimeoutException("request timed out", ex);
        }
        catch (WebException ex) when (ex.Status == WebExceptionStatus.ProtocolError) {
          return ex.Response as HttpWebResponse;
        }
      }
    }
  }
}
