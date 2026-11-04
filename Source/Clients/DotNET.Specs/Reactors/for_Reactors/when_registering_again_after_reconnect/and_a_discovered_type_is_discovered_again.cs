// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_again_after_reconnect;

public class and_a_discovered_type_is_discovered_again : for_Reactors.given.an_observed_reactor_stream
{
    async Task Establish()
    {
        _connectionLifecycle.IsConnected.Returns(true);
        _clientArtifactsProvider.Reactors.Returns([typeof(DiscoveredReactor)]);
        await _reactors.Discover();
        await _reactors.Register();
        _connectionLifecycle.OnDisconnected += Raise.Event<Disconnected>();
    }

    async Task Because()
    {
        _connectionLifecycle.IsConnected.Returns(false);
        await _reactors.Discover();
        _connectionLifecycle.IsConnected.Returns(true);
        await _reactors.RegisterAgainAfterReconnect();
    }

    [Fact] void should_keep_the_pending_registration() => _definitions.Count.ShouldEqual(2);

    class DiscoveredReactor : IReactor;
}
