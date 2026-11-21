// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_the_constraint_index_is_unavailable;

public class and_seeding_runs_again : given.an_event_seeding_grain
{
    SeedingEntry[] _entries;
    SeedingResult _firstResult;
    SeedingResult _secondResult;

    void Establish()
    {
        _entries = [AnEntry(1), AnEntry(2)];
        AppendManyReturns(call => call < 5 ? AnIndexUnavailableResult() : ASuccessfulResult());
    }

    async Task Because()
    {
        _firstResult = await _grain.SeedWithResult(_entries);
        _secondResult = await _grain.SeedWithResult(_entries);
    }

    [Fact] void should_leave_the_first_run_incomplete() => _firstResult.ShouldEqual(SeedingResult.Incomplete);
    [Fact] void should_offer_the_deferred_batch_again() => AppendAttempts.ShouldEqual(6);
    [Fact] void should_record_the_pending_entries_by_event_type() => TrackedByEventType.Select(_ => _.Content).ShouldEqual(_entries.Select(_ => _.Content));
    [Fact] void should_record_the_pending_entries_by_event_source() => TrackedByEventSource.Select(_ => _.Content).ShouldEqual(_entries.Select(_ => _.Content));
    [Fact] void should_persist_only_after_success() => _state.Received(1).WriteStateAsync();
    [Fact] void should_complete_the_later_run() => _secondResult.ShouldEqual(SeedingResult.Complete);
}
