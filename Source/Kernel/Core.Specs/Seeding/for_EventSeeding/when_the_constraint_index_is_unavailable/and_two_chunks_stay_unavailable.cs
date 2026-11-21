// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_the_constraint_index_is_unavailable;

public class and_two_chunks_stay_unavailable : given.an_event_seeding_grain
{
    SeedingResult _result;

    void Establish() => AppendManyReturns(_ => AnIndexUnavailableResult());

    async Task Because() => _result = await _grain.SeedWithResult(Enumerable.Range(0, 150).Select(AnEntry));

    [Fact] void should_share_four_delays_across_both_chunks() => _retryDelays.ShouldEqual(Enumerable.Repeat(TimeSpan.FromSeconds(1), 4));
    [Fact] void should_try_the_second_chunk_without_resetting_the_budget() => AppendAttempts.ShouldEqual(6);
    [Fact] void should_not_record_any_entry_by_event_type() => TrackedByEventType.ShouldBeEmpty();
    [Fact] void should_not_record_any_entry_by_event_source() => TrackedByEventSource.ShouldBeEmpty();
    [Fact] void should_not_persist_unappended_entries() => _state.DidNotReceive().WriteStateAsync();
    [Fact] void should_report_incomplete_seeding() => _result.ShouldEqual(SeedingResult.Incomplete);
    [Fact] void should_warn_for_both_deferred_chunks() => LoggedLevels.ShouldEqual([LogLevel.Warning, LogLevel.Warning]);
}
