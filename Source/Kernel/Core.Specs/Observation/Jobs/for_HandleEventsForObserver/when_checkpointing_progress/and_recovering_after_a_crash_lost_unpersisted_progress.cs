// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_checkpointing_progress;

/// <summary>
/// Proves the debounce durability relaxation is safe: progress reported below the checkpoint interval is not
/// persisted, and after a crash the step resumes from the last persisted checkpoint and re-reads forward,
/// re-processing the events whose checkpoint was only advanced in memory. Because observers are idempotent, the
/// re-processed range loses nothing and applies nothing twice.
/// </summary>
public class and_recovering_after_a_crash_lost_unpersisted_progress : given.a_debouncing_job_step
{
    int _writesAfterReporting;

    async Task Because()
    {
        // Report progress below the checkpoint interval - the in-memory checkpoint advances but is not persisted.
        await _jobStep.ReportNewSuccessfullyHandledEvent(2UL);
        _writesAfterReporting = _stateStorageStats.Writes;

        // Model the crash and resume: the reloaded state still holds the last persisted checkpoint (nothing was
        // written above), so the step re-reads forward from there.
        _performState.LastSuccessfullyHandledEventSequenceNumber = EventSequenceNumber.Unavailable;
        await _jobStep.InvokePerformStep(_performState);
    }

    [Fact] void should_not_have_persisted_the_debounced_checkpoint_before_the_crash() => _writesAfterReporting.ShouldEqual(0);
    [Fact] void should_resume_re_reading_from_the_last_persisted_checkpoint() => _startSequenceNumber.ShouldEqual(EventSequenceNumber.First);
}
