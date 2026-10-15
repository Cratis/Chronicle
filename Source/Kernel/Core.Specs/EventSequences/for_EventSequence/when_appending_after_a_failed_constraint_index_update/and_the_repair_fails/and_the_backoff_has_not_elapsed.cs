// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_after_a_failed_constraint_index_update.and_the_repair_fails;

public class and_the_backoff_has_not_elapsed : given.an_event_sequence_with_repairable_indexes
{
    readonly given.RepairClock _clock = new();
    AppendResult _beforeBackoff;
    int _readsBeforeBackoff;

    async Task Establish()
    {
        _failWrites = true;
        await AppendAnEvent();
        _eventSequence.ConstraintIndexTimeProvider = _clock;
        for (var failure = 0; failure < 4; failure++)
        {
            await AppendAnEvent();
            _clock.Advance(TimeSpan.FromSeconds(5));
        }
        _rangeReads = 0;
        _failWrites = false;
    }

    async Task Because()
    {
        // Cadence elapsed after 5 seconds, but the fourth failure backs off for 8 seconds.
        _beforeBackoff = await AppendAnEvent();
        _readsBeforeBackoff = _rangeReads;
        _clock.Advance(TimeSpan.FromSeconds(3));
        _result = await AppendAnEvent();
    }

    [Fact] void should_refuse_after_cadence_but_before_backoff() => _beforeBackoff.Errors.ShouldContain(AppendError.ConstraintIndexUnavailable);
    [Fact] void should_not_read_the_range_before_backoff_expires() => _readsBeforeBackoff.ShouldEqual(0);
    [Fact] void should_resume_once_backoff_expires() => _result.Errors.ShouldBeEmpty();
}
