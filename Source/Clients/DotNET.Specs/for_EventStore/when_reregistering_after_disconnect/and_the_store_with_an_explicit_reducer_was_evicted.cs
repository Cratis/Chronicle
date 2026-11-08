// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reducers;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.for_EventStore.when_reregistering_after_disconnect;

public class and_the_store_with_an_explicit_reducer_was_evicted : given.a_store_with_an_explicit_reducer
{
    void Establish() => _store.DetachAutomaticRegistration();

    async Task Because()
    {
        await _connection.Lifecycle.Disconnected();
        await _connection.Lifecycle.Connected();
    }

    [Fact] void should_not_register_the_evicted_reducer_again() => _services.Reducers.Received(1).Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>());
}
