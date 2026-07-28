// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_debouncing_progress_persistence;

public class and_the_observer_deactivates_with_pending_progress : given.an_observer_debouncing_progress_persistence
{
    async Task Because()
    {
        // Two progress-only batches leave an advance debounced (below the interval), then a graceful shutdown
        // must flush it so the observer resumes from where it actually got to.
        await _observer.Handle("p1", [AppendedEvent.EmptyWithEventSequenceNumber(0UL)]);
        await _observer.Handle("p1", [AppendedEvent.EmptyWithEventSequenceNumber(1UL)]);
        await _observer.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.ShuttingDown, string.Empty), CancellationToken.None);
    }

    [Fact] void should_flush_the_pending_progress_on_deactivate() => _storageStats.Writes.ShouldEqual(1);
    [Fact] void should_persist_the_advanced_next_event_sequence_number() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)2UL);
}
