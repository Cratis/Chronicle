// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationRegistry;

public class when_bootstrapping_entries_that_were_removed_or_revoked : given.a_configured_registry
{
    IReadOnlyList<FederationBindingState> _bindings;
    IReadOnlyList<FederationGrantState> _grants;

    async Task Because()
    {
        await _registry.BootstrapFromConfiguration();
        await _registry.Unbind("orders");
        await _registry.Revoke("link-1");
        _appended.Clear();

        var restarted = CreateRegistry();
        await restarted.BootstrapFromConfiguration();
        _bindings = await restarted.GetBindings();
        _grants = await restarted.GetGrants();
    }

    [Fact] void should_not_append_anything() => _appended.ShouldBeEmpty();
    [Fact] void should_not_resurrect_the_removed_binding() => _bindings.ShouldBeEmpty();
    [Fact] void should_keep_the_grant_revoked() => _grants.Single().Revoked.ShouldBeTrue();
}
