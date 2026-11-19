// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_binding_configuration;

public class and_unseen_observer_removal_is_not_configured : Specification
{
    ChronicleOptions _options;

    void Establish() => _options = new ChronicleOptions();

    [Fact] void should_remove_observers_unseen_for_seven_days() => _options.Observers.UnseenRemovalAfter.ShouldEqual(TimeSpan.FromDays(7));
    [Fact] void should_sweep_every_hour() => _options.Observers.UnseenRemovalSweepInterval.ShouldEqual(TimeSpan.FromHours(1));
    [Fact] void should_read_a_hundred_observers_at_a_time() => _options.Observers.UnseenRemovalBatchSize.ShouldEqual(100);
}
