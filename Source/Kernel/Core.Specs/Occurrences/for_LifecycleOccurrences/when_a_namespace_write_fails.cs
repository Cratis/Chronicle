// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_LifecycleOccurrences;

public class when_a_namespace_write_fails : given.a_lifecycle_grain
{
    int _retryTimers;
    int _failedProgress;
    bool _unavailable = true;

    async Task Establish()
    {
        await _grain.Record(OccurrenceKind.Startup, "rollout", true);
        _queue.Prepare().Returns(_ => _unavailable ? Task.FromException(new IOException("unavailable namespace")) : Task.CompletedTask);
    }

    async Task Because()
    {
        await _silo.TimerRegistry.FireAllAsync();
        _retryTimers = _silo.TimerRegistry.NumberOfActiveTimers;
        _failedProgress = (await _intents.Get(OccurrenceIdentities.ForLifecycle(OccurrenceKind.Startup, "rollout")))!.Progress;
        _unavailable = false;
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_keep_recovery_armed() => _retryTimers.ShouldEqual(1);
    [Fact] void should_not_acknowledge_a_failed_prefix() => _failedProgress.ShouldEqual(0);
    [Fact] void should_disarm_after_recovery_succeeds() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
