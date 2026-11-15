// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationRegistry;

public class when_unbinding : given.a_federation_registry
{
    IReadOnlyList<FederationBindingState> _bindings;

    async Task Because()
    {
        await _registry.Bind(Binding("orders"));
        await _registry.Bind(Binding("billing"));
        await _registry.Unbind("orders");
        _bindings = await _registry.GetBindings();
    }

    [Fact] void should_append_an_unbound_event_on_the_binding_id() => _appended[^1].Id.ShouldEqual("orders");
    [Fact] void should_append_a_peer_unbound_event() => _appended[^1].Content.ShouldBeOfExactType<FederationPeerUnbound>();
    [Fact] void should_remove_only_that_binding() => _bindings.Select(_ => _.Id).ShouldContainOnly(["billing"]);
}
