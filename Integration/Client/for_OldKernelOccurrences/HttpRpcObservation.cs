// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

namespace Cratis.Chronicle.Integration.for_OldKernelOccurrences;

/// <summary>
/// Observes actual HTTP sends to one kernel without recording headers, payloads or query strings.
/// </summary>
public sealed class HttpRpcObservation : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>, IDisposable
{
    readonly int _port;
    readonly ConcurrentQueue<string> _paths = new();
    readonly ConcurrentBag<IDisposable> _subscriptions = [];
    readonly IDisposable _listeners;

    public HttpRpcObservation(int port)
    {
        _port = port;
        _listeners = DiagnosticListener.AllListeners.Subscribe(this);
    }

    public IEnumerable<string> Paths => _paths.ToArray();

    public void OnNext(DiagnosticListener value)
    {
        if (value.Name == "HttpHandlerDiagnosticListener") _subscriptions.Add(value.Subscribe(this));
    }

    public void OnNext(KeyValuePair<string, object?> value)
    {
        if (value.Key != "System.Net.Http.HttpRequestOut.Start") return;
        var request = value.Value?.GetType().GetProperty("Request", BindingFlags.Instance | BindingFlags.Public)?.GetValue(value.Value) as HttpRequestMessage;
        if (request?.RequestUri is { } uri && uri.Port == _port && uri.IsLoopback) _paths.Enqueue(uri.AbsolutePath);
    }

    public void OnCompleted() { }
    public void OnError(Exception error) => throw new InvalidOperationException("HTTP diagnostic observation failed.", error);

    public void Dispose()
    {
        _listeners.Dispose();
        foreach (var subscription in _subscriptions) subscription.Dispose();
    }
}
