// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_the_constraint_index_is_unavailable;

public class and_a_constraint_violation_follows_unavailability : given.an_event_seeding_grain
{
    SeedingResult _result;

    void Establish() => AppendManyReturns(call => call == 0 ? AnIndexUnavailableResult() : AViolatedResult());

    async Task Because() => _result = await _grain.SeedWithResult([AnEntry(1)]);

    [Fact] void should_stop_retrying_after_the_constraint_violation() => AppendAttempts.ShouldEqual(2);
    [Fact] void should_wait_only_for_the_transient_refusal() => _retryDelays.ShouldEqual([TimeSpan.FromSeconds(1)]);
    [Fact] void should_not_record_any_entry_by_event_type() => TrackedByEventType.ShouldBeEmpty();
    [Fact] void should_not_record_any_entry_by_event_source() => TrackedByEventSource.ShouldBeEmpty();
    [Fact] void should_not_persist_unappended_entries() => _state.DidNotReceive().WriteStateAsync();
    [Fact] void should_report_incomplete_seeding() => _result.ShouldEqual(SeedingResult.Incomplete);
    [Fact] void should_report_the_constraint_violation_as_an_error() => LoggedLevels.ShouldEqual([LogLevel.Error]);
}
