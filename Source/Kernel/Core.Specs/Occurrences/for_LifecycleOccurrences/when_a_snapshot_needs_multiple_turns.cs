// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_LifecycleOccurrences;

public class when_a_snapshot_needs_multiple_turns : given.a_lifecycle_grain
{
    int _remainingTimers;
    int _prefix;

    async Task Establish()
    {
        for (var index = 0; index < 99; index++) AddNamespace($"tenant-{index}");
        await _grain.Record(OccurrenceKind.Startup, "rollout", true);
    }

    async Task Because()
    {
        await _silo.TimerRegistry.FireAllAsync();
        _remainingTimers = _silo.TimerRegistry.NumberOfActiveTimers;
        _prefix = (await _intents.Get(OccurrenceIdentities.ForLifecycle(OccurrenceKind.Startup, "rollout")))!.Progress;
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_bound_the_first_turn() => _prefix.ShouldEqual(100);
    [Fact] void should_keep_recovery_armed_for_the_remaining_namespace() => _remainingTimers.ShouldEqual(1);
    [Fact] void should_disarm_after_the_last_namespace() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
