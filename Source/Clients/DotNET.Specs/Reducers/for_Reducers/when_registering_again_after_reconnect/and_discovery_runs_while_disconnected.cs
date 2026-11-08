// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering_again_after_reconnect;

public class and_discovery_runs_while_disconnected : given.a_connected_reducers
{
    async Task Establish()
    {
        _clientArtifacts.Reducers.Returns([typeof(First)]);
        await _reducers.Discover();
        await _reducers.Register();
        _lifecycle.OnDisconnected += Raise.Event<Disconnected>();
    }

    async Task Because()
    {
        await _reducers.Discover();
        await _reducers.RegisterAgainAfterReconnect();
    }

    [Fact] void should_register_the_discovered_reducer_again() => _services.Reducers.Received(2).Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>());
}
