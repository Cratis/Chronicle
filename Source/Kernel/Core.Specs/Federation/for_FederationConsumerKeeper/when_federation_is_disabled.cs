// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation.for_FederationConsumerKeeper;

public class when_federation_is_disabled : given.a_link_pump
{
    IGrainFactory _grains;
    FederationConsumerKeeper _keeper;
    FederationConsumerKeeper _withoutRegistry;

    void Establish()
    {
        _grains = Substitute.For<IGrainFactory>();
        var disabled = Options.Create(new ChronicleOptions { Federation = new Cratis.Chronicle.Configuration.Federation { Enabled = false } });
        _keeper = new(_grains, NullLogger<FederationConsumerKeeper>.Instance, disabled, _registry);
        _withoutRegistry = new(_grains, NullLogger<FederationConsumerKeeper>.Instance, _options);
    }

    async Task Because()
    {
        await _keeper.Reconcile();
        await _withoutRegistry.Reconcile();
    }

    [Fact] void should_not_read_the_bindings() => _registry.DidNotReceive().GetBindings();
    [Fact] void should_not_activate_any_consumer() => _grains.ReceivedCalls().ShouldBeEmpty();
}
