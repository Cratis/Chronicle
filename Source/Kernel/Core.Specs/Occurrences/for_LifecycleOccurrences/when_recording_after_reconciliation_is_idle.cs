// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_LifecycleOccurrences;

public class when_recording_after_reconciliation_is_idle : given.a_lifecycle_grain
{
    int _armed;

    Task Establish() => _silo.TimerRegistry.FireAllAsync();

    async Task Because()
    {
        await _grain.Record(OccurrenceKind.Startup, "rollout", true);
        _armed = _silo.TimerRegistry.NumberOfActiveTimers;
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_rearm_for_the_durable_intent() => _armed.ShouldEqual(1);
    [Fact] void should_disarm_after_the_prefix_completes() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
    [Fact] async Task should_deliver_to_every_namespace() => (await _intents.GetPending(16)).ShouldBeEmpty();
}
