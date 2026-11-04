// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering_again_after_reconnect;

public class and_the_connection_dropped_again_first : given.a_registered_reducer_that_was_disconnected
{
    async Task Because()
    {
        _lifecycle.OnDisconnected += Raise.Event<Disconnected>();
        await _reducers.RegisterAgainAfterReconnect();
    }

    [Fact] void should_keep_the_mark_and_register_once() => _services.Reducers.Received(2).Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>());
}
