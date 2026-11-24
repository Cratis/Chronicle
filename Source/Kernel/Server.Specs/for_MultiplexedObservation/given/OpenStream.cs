// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;

namespace Cratis.Chronicle.Server.for_MultiplexedObservation.given;

/// <summary>
/// A stream a spec opened through the client channel: what it sends, and what it was told.
/// </summary>
/// <typeparam name="TIn">The type of message the observer sends the kernel.</typeparam>
/// <typeparam name="TOut">The type of message the kernel sends the observer.</typeparam>
public sealed class OpenStream<TIn, TOut> : IDisposable
{
    readonly ConcurrentQueue<TOut> _received = new();
    IDisposable? _subscription;

    public Subject<TIn> Messages { get; } = new();

    public IReadOnlyList<TOut> Received => [.. _received];

    public Exception? Error { get; private set; }

    public bool Completed { get; private set; }

    public Func<TOut, Task>? OnReceived { get; set; }

    public void Open(IObservable<TOut> observable) =>
        _subscription = observable.Subscribe(
            message =>
            {
                OnReceived?.Invoke(message).GetAwaiter().GetResult();
                _received.Enqueue(message);
            },
            error => Error = error,
            () => Completed = true);

    public void Dispose()
    {
        _subscription?.Dispose();
        Messages.Dispose();
    }
}
