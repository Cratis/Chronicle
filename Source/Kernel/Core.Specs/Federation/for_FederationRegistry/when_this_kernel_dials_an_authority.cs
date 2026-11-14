// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Federation.for_FederationRegistry;

public class when_this_kernel_dials_an_authority : given.a_federation_registry
{
    IReadOnlyList<FederationBindingState> _bindings;

    async Task Establish()
    {
        await _registry.Bind(Binding("orders"));
        _options = new()
        {
            Federation = new Cratis.Chronicle.Configuration.Federation
            {
                Dial = new FederationDialOptions { Enabled = true, EventStore = "customer", Namespace = "prod", LogicalSource = "platform" }
            }
        };
        _registry = CreateRegistry();
    }

    async Task Because() => _bindings = await _registry.GetBindings();

    [Fact] void should_keep_the_bindings_that_were_declared() => _bindings.Any(_ => _.Id == "orders").ShouldBeTrue();
    [Fact] void should_add_the_binding_of_the_dialer_link() => _bindings.Single(_ => _.Id == FederationDial.BindingId).LogicalSource.ShouldEqual("platform");
    [Fact] void should_present_the_enrolled_certificate() => FederationDial.IsDial(_bindings.Single(_ => _.Id == FederationDial.BindingId)).ShouldBeTrue();
    [Fact] void should_target_the_configured_local_store() => _bindings.Single(_ => _.Id == FederationDial.BindingId).TargetEventStore.ShouldEqual("customer");
    [Fact] void should_not_name_a_target_on_the_authority() => _bindings.Single(_ => _.Id == FederationDial.BindingId).RemoteEventStore.ShouldBeEmpty();
    [Fact] void should_pin_nothing_because_the_authority_root_is_the_trust() => _bindings.Single(_ => _.Id == FederationDial.BindingId).ServerPin.ShouldBeEmpty();
}
