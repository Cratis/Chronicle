// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_binding_configuration;

public class and_dormant_kinds_are_configured : Specification
{
    ChronicleOptions _configured;
    ChronicleOptions _default;

    void Establish()
    {
        _default = new ChronicleOptions();
        _configured = new ChronicleOptions();
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cratis:Chronicle:Observers:DormantKinds:0"] = "ClientReactor",
                ["Cratis:Chronicle:Observers:DormantKinds:1"] = "Projection"
            })
            .Build()
            .GetSection(ChronicleOptions.SectionPath)
            .Bind(_configured);
    }

    [Fact] void should_bind_the_configured_kinds() => _configured.Observers.EffectiveDormantKinds.ShouldContainOnly(ObserverSubscriptionKind.ClientReactor, ObserverSubscriptionKind.Projection);
    [Fact] void should_release_event_store_subscriptions_by_default() => _default.Observers.EffectiveDormantKinds.ShouldContain(ObserverSubscriptionKind.EventStoreSubscription);
    [Fact] void should_never_release_webhooks_by_default() => _default.Observers.EffectiveDormantKinds.ShouldNotContain(ObserverSubscriptionKind.Webhook);
}
