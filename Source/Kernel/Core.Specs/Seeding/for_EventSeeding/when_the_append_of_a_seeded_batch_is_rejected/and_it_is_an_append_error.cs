// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_the_append_of_a_seeded_batch_is_rejected;

/// <summary>
/// An append error is the other half of the failure surface - anything that is not a constraint violation,
/// including an exception the event sequence caught and turned into a result. It is just as much a
/// "nothing was appended" answer as a violation is, and must be treated the same way.
/// </summary>
public class and_it_is_an_append_error : given.an_event_seeding_grain
{
    IEnumerable<SeedingEntry> _entries;

    void Establish()
    {
        _entries = [AnEntry(1), AnEntry(2)];
        AppendManyReturns(_ => AnErroredResult());
    }

    async Task Because() => await _grain.Seed(_entries);

    [Fact] void should_not_record_any_entry_as_seeded_by_event_type() => TrackedByEventType.ShouldBeEmpty();
    [Fact] void should_not_record_any_entry_as_seeded_by_event_source() => TrackedByEventSource.ShouldBeEmpty();
    [Fact] void should_not_persist_a_claim_it_did_not_make() => _state.DidNotReceive().WriteStateAsync();
    [Fact] void should_report_the_rejection_as_an_error() => LoggedLevel.ShouldEqual(LogLevel.Error);
}
