// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_LifecycleOccurrences;

public class when_ensuring_with_a_pending_intent : given.a_lifecycle_grain
{
    int _timers;

    async Task Establish()
    {
        await _grain.Record(OccurrenceKind.Startup, "rollout", true);
        _queue.Prepare().Returns(Task.FromException(new IOException("unavailable namespace")));
        await _silo.TimerRegistry.FireAllAsync();
    }

    async Task Because()
    {
        await _grain.Ensure();
        _timers = _silo.TimerRegistry.NumberOfActiveTimers;
    }

    [Fact] void should_keep_reconciliation_armed() => _timers.ShouldEqual(1);
}
