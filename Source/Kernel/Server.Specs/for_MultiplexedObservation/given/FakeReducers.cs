// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Server.for_MultiplexedObservation.given;

/// <summary>
/// Stands in for the reducers service of the kernel: records what each observe call is sent and lets a spec say what it emits.
/// </summary>
public class FakeReducers : IReducers
{
    readonly ConcurrentQueue<FakeObserveCall<ReducerMessage, ReduceOperationMessage>> _calls = new();

    public IReadOnlyList<FakeObserveCall<ReducerMessage, ReduceOperationMessage>> Calls => [.. _calls];

    public IObservable<ReduceOperationMessage> Observe(IObservable<ReducerMessage> messages, CallContext context = default)
    {
        var call = new FakeObserveCall<ReducerMessage, ReduceOperationMessage>();
        _calls.Enqueue(call);
        return Observable.Create<ReduceOperationMessage>(observer =>
        {
            var received = messages.Subscribe(call.Receive);
            call.Attach(observer);
            return Disposable.Create(() =>
            {
                received.Dispose();
                call.Detach();
            });
        });
    }
}
