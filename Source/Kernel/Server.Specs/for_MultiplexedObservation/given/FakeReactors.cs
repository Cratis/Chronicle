// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Server.for_MultiplexedObservation.given;

/// <summary>
/// Stands in for the reactors service of the kernel: records what each observe call is sent and lets a spec say what it emits.
/// </summary>
public class FakeReactors : IReactors
{
    readonly ConcurrentQueue<FakeObserveCall<ReactorMessage, EventsToObserve>> _calls = new();

    public IReadOnlyList<FakeObserveCall<ReactorMessage, EventsToObserve>> Calls => [.. _calls];

    public IObservable<EventsToObserve> Observe(IObservable<ReactorMessage> messages, CallContext context = default)
    {
        var call = new FakeObserveCall<ReactorMessage, EventsToObserve>();
        _calls.Enqueue(call);
        return Observable.Create<EventsToObserve>(observer =>
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

    public Task<HasReactorResponse> HasReactor(HasReactorRequest request, CallContext context = default) => Task.FromResult(new HasReactorResponse());
}
