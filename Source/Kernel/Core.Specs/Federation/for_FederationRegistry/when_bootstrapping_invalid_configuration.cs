// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Federation.for_FederationRegistry;

public class when_bootstrapping_invalid_configuration : given.a_federation_registry
{
    IReadOnlyList<FederationBindingState> _bindings;
    IReadOnlyList<FederationGrantState> _grants;

    void Establish()
    {
        _options = new()
        {
            Federation = new()
            {
                Bindings = new Dictionary<string, FederationBindingConfiguration>
                {
                    ["broken"] = new() { Endpoint = "http://insecure.example.com" }
                },
                Grants = new Dictionary<string, FederationGrantConfiguration>
                {
                    ["also-broken"] = new() { ClientPin = "nope", EventStore = "orders" }
                }
            }
        };
        _registry = CreateRegistry();
    }

    async Task Because()
    {
        await _registry.BootstrapFromConfiguration();
        _bindings = await _registry.GetBindings();
        _grants = await _registry.GetGrants();
    }

    [Fact] void should_not_append_anything() => _appended.ShouldBeEmpty();
    [Fact] void should_not_fail_the_kernel_startup_with_a_binding() => _bindings.ShouldBeEmpty();
    [Fact] void should_not_fail_the_kernel_startup_with_a_grant() => _grants.ShouldBeEmpty();
}
