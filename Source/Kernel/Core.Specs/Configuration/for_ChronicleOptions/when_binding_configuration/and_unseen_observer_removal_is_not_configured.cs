// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_binding_configuration;

public class and_unseen_observer_removal_is_not_configured : Specification
{
    ChronicleOptions _options;

    void Establish() => _options = new ChronicleOptions();

    [Fact] void should_not_remove_anything() => _options.Observers.UnseenRemovalEnabled.ShouldBeFalse();
    [Fact] void should_recommend_seven_days_when_turned_on() => _options.Observers.UnseenRemovalAfter.ShouldEqual(TimeSpan.FromDays(7));
    [Fact] void should_sweep_every_hour() => _options.Observers.UnseenRemovalSweepInterval.ShouldEqual(TimeSpan.FromHours(1));
    [Fact] void should_read_a_hundred_observers_at_a_time() => _options.Observers.UnseenRemovalBatchSize.ShouldEqual(100);
}
