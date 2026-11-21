// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_the_constraint_index_is_unavailable;

public class and_it_stays_unavailable : given.an_event_seeding_grain
{
    SeedingResult _result;

    void Establish() => AppendManyReturns(_ => AnIndexUnavailableResult());

    async Task Because() => _result = await _grain.SeedWithResult([AnEntry(1), AnEntry(2)]);

    [Fact] void should_bound_the_number_of_attempts() => AppendAttempts.ShouldEqual(5);
    [Fact] void should_wait_only_between_attempts() => _retryDelays.ShouldEqual(Enumerable.Repeat(TimeSpan.FromSeconds(1), 4));
    [Fact] void should_not_record_any_entry_by_event_type() => TrackedByEventType.ShouldBeEmpty();
    [Fact] void should_not_record_any_entry_by_event_source() => TrackedByEventSource.ShouldBeEmpty();
    [Fact] void should_not_persist_unappended_entries() => _state.DidNotReceive().WriteStateAsync();
    [Fact] void should_report_incomplete_seeding() => _result.ShouldEqual(SeedingResult.Incomplete);
    [Fact] void should_warn_that_the_batch_is_deferred() => LoggedLevels.ShouldContain(LogLevel.Warning);
    [Fact] void should_not_report_a_deterministic_rejection() => LoggedLevels.ShouldNotContain(LogLevel.Error);
}
