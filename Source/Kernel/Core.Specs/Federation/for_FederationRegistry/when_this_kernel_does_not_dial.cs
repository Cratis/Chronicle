// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Federation.for_FederationRegistry;

public class when_this_kernel_does_not_dial : given.a_federation_registry
{
    IReadOnlyList<FederationBindingState> _default;
    IReadOnlyList<FederationBindingState> _withoutLocalStore;

    async Task Establish() => await _registry.Bind(Binding("orders"));

    async Task Because()
    {
        _default = await _registry.GetBindings();
        _options = new()
        {
            Federation = new Cratis.Chronicle.Configuration.Federation { Dial = new FederationDialOptions { Enabled = true } }
        };
        _registry = CreateRegistry();
        _withoutLocalStore = await _registry.GetBindings();
    }

    [Fact] void should_add_nothing_by_default() => _default.Select(_ => _.Id).ToArray().ShouldEqual(["orders"]);
    [Fact] void should_add_nothing_when_no_local_store_is_configured() => _withoutLocalStore.Select(_ => _.Id).ToArray().ShouldEqual(["orders"]);
}
