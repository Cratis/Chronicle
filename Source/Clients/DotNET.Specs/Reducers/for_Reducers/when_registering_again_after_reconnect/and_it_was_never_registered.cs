// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering_again_after_reconnect;

public class and_it_was_never_registered : given.a_connected_reducers
{
    async Task Establish()
    {
        _clientArtifacts.Reducers.Returns([typeof(First)]);
        await _reducers.Discover();
        _lifecycle.OnDisconnected += Raise.Event<Disconnected>();
    }

    Task Because() => _reducers.RegisterAgainAfterReconnect();

    [Fact] void should_not_register_the_reducer() => _services.Reducers.DidNotReceive().Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>());
}
