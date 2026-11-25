// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation.for_FederationConsumerKeeper;

public class when_only_dialing : given.a_link_pump
{
    IGrainFactory _grains;
    IFederationConsumer _consumer;
    FederationConsumerKeeper _keeper;

    void Establish()
    {
        _grains = Substitute.For<IGrainFactory>();
        _consumer = Substitute.For<IFederationConsumer>();
        _grains.GetGrain<IFederationConsumer>(BindingId, null).Returns(_consumer);
        var dialOnly = Options.Create(new ChronicleOptions
        {
            Federation = new Cratis.Chronicle.Configuration.Federation { Enabled = false, Dial = new FederationDialOptions { Enabled = true } }
        });
        _keeper = new(_grains, NullLogger<FederationConsumerKeeper>.Instance, dialOnly, _registry);
    }

    async Task Because() => await _keeper.Reconcile();

    [Fact] void should_keep_the_consumers_running_without_a_listener() => _consumer.Received(1).Ensure();
}
