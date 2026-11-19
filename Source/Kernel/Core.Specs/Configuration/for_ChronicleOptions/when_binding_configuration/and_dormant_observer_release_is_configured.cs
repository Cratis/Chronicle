// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_binding_configuration;

public class and_dormant_observer_release_is_configured : Specification
{
    ChronicleOptions _options;

    void Establish()
    {
        _options = new ChronicleOptions();
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cratis:Chronicle:Observers:DormantAfter"] = "300",
                ["Cratis:Chronicle:Observers:DormantSafetyNet"] = "3600",
                ["Cratis:Chronicle:Observers:DurableSubscriptions"] = "Off",
                ["Cratis:Chronicle:Observers:MaxConcurrentWakes"] = "8",
                ["Cratis:Chronicle:Observers:DurableSubscriptionCheckInterval"] = "00:05:00"
            })
            .Build()
            .GetSection(ChronicleOptions.SectionPath)
            .Bind(_options);
    }

    [Fact] void should_bind_the_dormant_period() => _options.Observers.DormantAfter.ShouldEqual(300);
    [Fact] void should_bind_the_safety_net() => _options.Observers.DormantSafetyNet.ShouldEqual(3600);
    [Fact] void should_bind_the_durable_subscription_mode() => _options.Observers.DurableSubscriptions.ShouldEqual(DurableSubscriptionMode.Off);
    [Fact] void should_bind_the_wake_limit() => _options.Observers.MaxConcurrentWakes.ShouldEqual(8);
    [Fact] void should_bind_the_check_interval() => _options.Observers.DurableSubscriptionCheckInterval.ShouldEqual(TimeSpan.FromMinutes(5));
}
