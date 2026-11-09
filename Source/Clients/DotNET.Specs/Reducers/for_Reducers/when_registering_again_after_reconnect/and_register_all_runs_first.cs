// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reducers;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering_again_after_reconnect;

public class and_register_all_runs_first : given.a_registered_reducer_that_was_disconnected
{
    async Task Because()
    {
        await _reducers.Register();
        await _reducers.RegisterAgainAfterReconnect();
    }

    [Fact] void should_not_register_a_duplicate() => _services.Reducers.Received(2).Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>());
}
