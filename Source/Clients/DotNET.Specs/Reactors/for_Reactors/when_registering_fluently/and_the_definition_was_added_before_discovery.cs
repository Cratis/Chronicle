// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_fluently;

public class and_the_definition_was_added_before_discovery : given.an_observed_reactor_stream
{
    int _registeredBeforeDiscovery;

    void Establish()
    {
        _reactors.Add("orders", reactor => reactor.On<OrderPlaced>(_ => { }));
        _registeredBeforeDiscovery = _definitions.Count;
    }

    async Task Because()
    {
        await _reactors.Discover();
        await _reactors.Register();
    }

    [Fact] void should_not_register_before_the_registration_pass() => _registeredBeforeDiscovery.ShouldEqual(0);
    [Fact] void should_register_in_the_registration_pass() => _definitions.Single().ReactorId.ShouldEqual("orders");
    [Fact] void should_be_reachable_by_its_identifier() => _reactors.GetHandlerById("orders").EventTypes.ShouldContainOnly([_orderPlaced]);
}
