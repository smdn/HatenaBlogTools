// SPDX-FileCopyrightText: 2022 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#nullable enable

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Smdn.HatenaBlogTools.AtomPublishingProtocol;

internal class PseudoAtomPubServer : IDisposable {
  private HttpListener? listener;

  public Uri Url { get; } = new Uri("http://localhost:14730");

  private void ThrowIfDisposed()
  {
    if (listener is null)
      throw new ObjectDisposedException(GetType().FullName);
  }

  public void Dispose()
  {
    if (listener is not null) {
      (listener as IDisposable)?.Dispose();
      listener = null;
    }
  }

  public void Start()
  {
    if (!HttpListener.IsSupported)
      throw new InvalidOperationException($"{nameof(HttpListener)} is not supported");

    if (listener is not null)
      throw new InvalidOperationException("already started");

    try {
      listener = new HttpListener();
      listener.Prefixes.Add(Url.AbsoluteUri);

      listener.Start();

      if (!listener.IsListening)
        throw new InvalidOperationException("server not listening");
    }
    catch {
      listener?.Stop();
      listener = null;
    }
  }

  public void Stop()
  {
    ThrowIfDisposed();

    listener!.Stop();

    Dispose();
  }

  private HttpStatusCode nextResponseStatus = (HttpStatusCode)0;
  private string? nextResponseContent = null;

  public void SetNextResponseContent(HttpStatusCode status, string content)
  {
    nextResponseStatus = status;
    nextResponseContent = content;
  }

  private static readonly Encoding utf8nobom = new UTF8Encoding(false);

  public Task<(HttpListenerRequest, Stream)> ProcessRequestAsync()
  {
    ThrowIfDisposed();

    return Task.Run(() => Core(listener!));

    (HttpListenerRequest, Stream) Core(HttpListener l)
    {
      var context = l!.GetContext();

      var requestStream = new MemoryStream();

      context.Request.InputStream.CopyTo(requestStream);

      requestStream.Position = 0L;

      var response = context.Response;

      response.ContentEncoding = utf8nobom;
      response.KeepAlive = false;
      response.ContentType = "application/xml";

      try {
        using var s = new MemoryStream();

        using (var writer = new StreamWriter(s, response.ContentEncoding, 1024, true)) {
          if (nextResponseContent is null) {
            writer.WriteLine("response content not set");
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
          }
          else {
            writer.Write(nextResponseContent);
            response.StatusCode = (int)nextResponseStatus;
          }

          writer.Flush();
        }

        response.ContentLength64 = s.Length;

        s.Position = 0L;

        s.CopyTo(response.OutputStream);
      }
      finally {
        if (context != null)
          context.Response.OutputStream.Close();
      }

      return (context.Request, requestStream);
    }
  }
}
