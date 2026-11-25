// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_the_constraint_index_is_unavailable;

public class and_it_becomes_available_on_a_retry : given.an_event_seeding_grain
{
    SeedingEntry[] _entries;
    SeedingResult _result;

    void Establish()
    {
        _entries = [AnEntry(1), AnEntry(2)];
        AppendManyReturns(call => call < 2 ? AnIndexUnavailableResult() : ASuccessfulResult());
    }

    async Task Because() => _result = await _grain.SeedWithResult(_entries);

    [Fact] void should_retry_until_the_index_is_available() => AppendAttempts.ShouldEqual(3);
    [Fact] void should_honor_the_suggested_delay_before_each_retry() => _retryDelays.ShouldEqual([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)]);
    [Fact] void should_record_every_entry_by_event_type() => TrackedByEventType.Select(_ => _.Content).ShouldEqual(_entries.Select(_ => _.Content));
    [Fact] void should_record_every_entry_by_event_source() => TrackedByEventSource.Select(_ => _.Content).ShouldEqual(_entries.Select(_ => _.Content));
    [Fact] void should_persist_the_successfully_seeded_entries() => _state.Received(1).WriteStateAsync();
    [Fact] void should_report_complete_seeding() => _result.ShouldEqual(SeedingResult.Complete);
    [Fact] void should_not_report_an_error() => LoggedLevels.ShouldNotContain(LogLevel.Error);
    [Fact] void should_not_report_a_deferral() => LoggedLevels.ShouldNotContain(LogLevel.Warning);
}
