// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_the_append_of_a_seeded_batch_is_rejected;

/// <summary>
/// Appending many validates the whole batch before writing anything and returns on the first failure, so a
/// single rejected event means none of the batch was appended. Recording the batch as seeded anyway is a
/// claim about events that do not exist, and it is permanent: the entries are skipped on every later run.
/// </summary>
public class and_it_is_a_constraint_violation : given.an_event_seeding_grain
{
    IEnumerable<SeedingEntry> _entries;

    void Establish()
    {
        _entries = [AnEntry(1), AnEntry(2), AnEntry(3)];
        AppendManyReturns(_ => AViolatedResult());
    }

    async Task Because() => await _grain.Seed(_entries);

    [Fact] void should_not_record_any_entry_as_seeded_by_event_type() => TrackedByEventType.ShouldBeEmpty();
    [Fact] void should_not_record_any_entry_as_seeded_by_event_source() => TrackedByEventSource.ShouldBeEmpty();
    [Fact] void should_not_persist_a_claim_it_did_not_make() => _state.DidNotReceive().WriteStateAsync();
    [Fact] void should_not_be_silent_about_the_rejection() => _logger.ReceivedWithAnyArgs(1).Log(LogLevel.Error, default, default(object)!, default, default!);
    [Fact] void should_report_the_rejection_as_an_error() => LoggedLevel.ShouldEqual(LogLevel.Error);
}
