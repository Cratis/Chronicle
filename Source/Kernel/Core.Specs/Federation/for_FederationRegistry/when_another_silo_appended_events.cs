// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationRegistry;

public class when_another_silo_appended_events : given.a_federation_registry
{
    IReadOnlyList<FederationBindingState> _before;
    IReadOnlyList<FederationBindingState> _after;

    async Task Because()
    {
        _before = await _registry.GetBindings();
        Seed("orders", new FederationPeerBound("orders-service", "https://peer.example.com:35100", _pin, "client-cert", "orders", "Default", "local-orders", "Default", FederationOrigin.Runtime));
        _after = await _registry.GetBindings();
    }

    [Fact] void should_start_empty() => _before.ShouldBeEmpty();
    [Fact] void should_converge_on_the_next_read() => _after.Single().Id.ShouldEqual("orders");
}
