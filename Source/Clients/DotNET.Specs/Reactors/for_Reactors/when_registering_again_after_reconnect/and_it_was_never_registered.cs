// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_again_after_reconnect;

public class and_it_was_never_registered : for_Reactors.given.an_observed_reactor_stream
{
    async Task Establish()
    {
        _connectionLifecycle.IsConnected.Returns(true);
        _reactors.Add("unregistered", builder => builder.On<OrderPlaced>(_ => { }));
        await _reactors.Discover();
        _connectionLifecycle.OnDisconnected += Raise.Event<Disconnected>();
    }

    Task Because() => _reactors.RegisterAgainAfterReconnect();

    [Fact] void should_not_register_the_discovered_reactor() => _definitions.ShouldBeEmpty();
}
