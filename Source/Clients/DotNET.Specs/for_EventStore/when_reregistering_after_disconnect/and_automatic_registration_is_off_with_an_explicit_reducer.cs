// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reducers;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.for_EventStore.when_reregistering_after_disconnect;

public class and_automatic_registration_is_off_with_an_explicit_reducer : given.a_store_with_an_explicit_reducer
{
    async Task Because()
    {
        await _connection.Lifecycle.Disconnected();
        await _connection.Lifecycle.Connected();
    }

    [Fact] void should_register_the_reducer_on_the_new_connection() => _services.Reducers.Received(2).Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>());
    [Fact] void should_hand_out_a_recreated_handler() => ReferenceEquals(_store.Reducers.GetHandlerFor<Classic>(), _original).ShouldBeFalse();
    [Fact] void should_not_cancel_the_recreated_handler() => _store.Reducers.GetHandlerFor<Classic>().CancellationToken.IsCancellationRequested.ShouldBeFalse();
}
