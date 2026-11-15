// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationRegistry;

public class when_bootstrapping_with_runtime_state : given.a_configured_registry
{
    IReadOnlyList<FederationBindingState> _bindings;
    IReadOnlyList<FederationGrantState> _grants;

    async Task Because()
    {
        await _registry.Bind(Binding("orders", "https://runtime.example.com:35100"));
        await _registry.SetGrant(Grant("link-1", allPublic: false, eventTypes: ["order-placed"]));
        _appended.Clear();
        await _registry.BootstrapFromConfiguration();
        _bindings = await _registry.GetBindings();
        _grants = await _registry.GetGrants();
    }

    [Fact] void should_not_append_anything() => _appended.ShouldBeEmpty();
    [Fact] void should_keep_the_runtime_binding() => _bindings.Single().Endpoint.ShouldEqual("https://runtime.example.com:35100");
    [Fact] void should_keep_the_runtime_grant() => _grants.Single().AllPublic.ShouldBeFalse();
}
