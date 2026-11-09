// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationRegistry;

public class when_bootstrapping_with_no_history : given.a_configured_registry
{
    IReadOnlyList<FederationBindingState> _bindings;
    IReadOnlyList<FederationGrantState> _grants;

    async Task Because()
    {
        await _registry.BootstrapFromConfiguration();
        await _registry.BootstrapFromConfiguration();
        _bindings = await _registry.GetBindings();
        _grants = await _registry.GetGrants();
    }

    [Fact] void should_append_one_event_per_entry_even_when_run_twice() => _appended.Count.ShouldEqual(2);
    [Fact] void should_record_configuration_origin_for_the_binding() => _bindings.Single().Origin.ShouldEqual(FederationOrigin.Configuration);
    [Fact] void should_record_configuration_origin_for_the_grant() => _grants.Single().Origin.ShouldEqual(FederationOrigin.Configuration);
    [Fact] void should_only_refer_to_the_certificate_by_name() => _bindings.Single().ClientCertificate.ShouldEqual("client-cert");
}
