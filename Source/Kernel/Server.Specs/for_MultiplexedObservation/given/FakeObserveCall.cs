// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Chronicle.Server.for_MultiplexedObservation.given;

/// <summary>
/// One call of the fake reactors or reducers service.
/// </summary>
/// <typeparam name="TIn">The type of message the client sends.</typeparam>
/// <typeparam name="TOut">The type of message the call emits.</typeparam>
public class FakeObserveCall<TIn, TOut>
{
    readonly ConcurrentQueue<TIn> _received = new();
    readonly TaskCompletionSource _attached = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _detached = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IObserver<TOut>? _observer;

    public IReadOnlyList<TIn> Received => [.. _received];

    public Task Attached => _attached.Task;

    public Task Detached => _detached.Task;

    public void Receive(TIn message) => _received.Enqueue(message);

    public void Attach(IObserver<TOut> observer)
    {
        _observer = observer;
        _attached.TrySetResult();
    }

    public void Detach() => _detached.TrySetResult();

    public void Emit(TOut message) => _observer!.OnNext(message);

    public void Fail(Exception error) => _observer!.OnError(error);

    public void Complete() => _observer!.OnCompleted();
}
