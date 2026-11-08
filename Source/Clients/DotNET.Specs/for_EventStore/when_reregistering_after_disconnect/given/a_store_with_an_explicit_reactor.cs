// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.for_EventStore.when_reregistering_after_disconnect.given;

public class a_store_with_an_explicit_reactor : when_preparing_for_registration.given.an_explicit_store
{
    protected IReactorHandler _original;
    protected virtual bool AutomaticRegistration => false;

    async Task Establish()
    {
        _services.Reactors.Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>())
            .Returns(Observable.Never<EventsToObserve>());
        _auto = AutomaticRegistration;
        _store = CreateStore();
        await _connection.Lifecycle.Connected();
        _original = await _store.Reactors.Register("bridge", builder => builder.WithEventType(new EventType("orders", 1)), (_, _) => Task.CompletedTask);
    }
}
