// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Integration.for_Webhooks.given;

/// <summary>
/// Dispatches webhook requests to independently leased test receivers, even if a singleton retains the first factory.
/// </summary>
public class TestHttpClientFactory : IHttpClientFactory
{
    readonly Dictionary<Uri, ReceiverLease> _receivers = new();
    readonly object _lock = new();

    /// <summary>
    /// Gets the process-stable factory registered by every webhook context.
    /// </summary>
    public static TestHttpClientFactory Instance { get; } = new();

    /// <summary>
    /// Registers a uniquely addressed receiver until its lease is disposed. The caller owns the handler.
    /// </summary>
    /// <param name="target">The receiver's unique target URI.</param>
    /// <param name="handler">The receiver that handles requests for this target.</param>
    /// <returns>The lease that unregisters only this receiver and cancels its pending requests.</returns>
    public IDisposable Register(Uri target, HttpMessageHandler handler)
    {
        var key = Normalize(target);
        lock (_lock)
        {
            var lease = new ReceiverLease(this, key, handler);
            _receivers.Add(key, lease);
            return lease;
        }
    }

    /// <inheritdoc/>
    public HttpClient CreateClient(string name) => new(new Dispatcher(this));

    static Uri Normalize(Uri target) => new(target.GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped));

    Task<HttpResponseMessage> Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ReceiverLease? receiver;
        lock (_lock)
        {
            _receivers.TryGetValue(Normalize(request.RequestUri!), out receiver);
        }

        // An expired target must never fall through to whichever context is now running.
        return receiver is null
            ? Task.FromResult(new HttpResponseMessage(HttpStatusCode.Gone))
            : receiver.Send(request, cancellationToken);
    }

    void Unregister(Uri target, ReceiverLease lease)
    {
        lock (_lock)
        {
            if (_receivers.TryGetValue(target, out var registered) && ReferenceEquals(registered, lease))
            {
                _receivers.Remove(target);
            }
        }
    }

    sealed class Dispatcher(TestHttpClientFactory factory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            factory.Send(request, cancellationToken);
    }

    sealed class ReceiverLease : IDisposable
    {
        readonly TestHttpClientFactory _factory;
        readonly Uri _target;
        readonly HttpMessageInvoker _receiver;
        readonly CancellationTokenSource _lifetime = new();
        readonly CancellationToken _lifetimeToken;
        int _disposed;

        public ReceiverLease(TestHttpClientFactory factory, Uri target, HttpMessageHandler handler)
        {
            _factory = factory;
            _target = target;
            _receiver = new(handler, disposeHandler: false);
            _lifetimeToken = _lifetime.Token;
        }

        public async Task<HttpResponseMessage> Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetimeToken);
            cancellation.Token.ThrowIfCancellationRequested();
            return await _receiver.SendAsync(request, cancellation.Token);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            _factory.Unregister(_target, this);
            _lifetime.Cancel();
            _receiver.Dispose();
            _lifetime.Dispose();
        }
    }
}
