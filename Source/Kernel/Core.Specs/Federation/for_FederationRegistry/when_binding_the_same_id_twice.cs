// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationRegistry;

public class when_binding_the_same_id_twice : given.a_federation_registry
{
    IReadOnlyList<FederationBindingState> _bindings;

    async Task Because()
    {
        await _registry.Bind(Binding(endpoint: "https://first.example.com:35100"));
        await _registry.Bind(Binding(endpoint: "https://second.example.com:35100"));
        _bindings = await _registry.GetBindings();
    }

    [Fact] void should_have_a_single_binding() => _bindings.Count.ShouldEqual(1);
    [Fact] void should_let_the_last_event_win() => _bindings[0].Endpoint.ShouldEqual("https://second.example.com:35100");
}
