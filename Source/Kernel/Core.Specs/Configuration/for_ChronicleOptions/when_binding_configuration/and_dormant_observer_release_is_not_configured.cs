// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_binding_configuration;

public class and_dormant_observer_release_is_not_configured : Specification
{
    ChronicleOptions _options;

    void Establish() => _options = new ChronicleOptions();

    [Fact] void should_release_dormant_observers_after_fifteen_minutes() => _options.Observers.DormantAfter.ShouldEqual(900);
    [Fact] void should_wake_dormant_observers_every_fifteen_minutes() => _options.Observers.DormantSafetyNet.ShouldEqual(900);
    [Fact] void should_record_durable_subscriptions_in_shadow() => _options.Observers.DurableSubscriptions.ShouldEqual(DurableSubscriptionMode.Shadow);
    [Fact] void should_choose_the_wake_limit_automatically() => _options.Observers.MaxConcurrentWakes.ShouldEqual(0);
    [Fact] void should_compare_durable_subscriptions_every_fifteen_minutes() => _options.Observers.DurableSubscriptionCheckInterval.ShouldEqual(TimeSpan.FromMinutes(15));
}
