// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_fluently;

public class and_the_definition_was_added_before_discovery_and_the_connection_is_re_established : given.an_observed_reactor_stream
{
    async Task Establish()
    {
        _reactors.Add("orders", reactor => reactor.On<OrderPlaced>(_ => { }));
        await _reactors.Discover();
        await _reactors.Register();
    }

    async Task Because()
    {
        _connectionLifecycle.OnDisconnected += Raise.Event<Disconnected>();
        await _reactors.Discover();
        await _reactors.Register();
    }

    [Fact] void should_register_again_on_the_new_connection() => _definitions.Count.ShouldEqual(2);
    [Fact] void should_register_the_same_reactor_both_times() => _definitions.TrueForAll(_ => _.ReactorId == "orders").ShouldBeTrue();
}
