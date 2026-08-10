// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_the_append_of_a_seeded_batch_is_rejected;

/// <summary>
/// Result-aware coordination needs to distinguish a rejected batch from complete seeding so its caller can leave
/// global tracking uncommitted and offer the entries again.
/// </summary>
public class and_the_result_is_requested : given.an_event_seeding_grain
{
    IEnumerable<SeedingEntry> _entries;
    SeedingResult _result;

    void Establish()
    {
        _entries = [AnEntry(1), AnEntry(2)];
        AppendManyReturns(_ => AViolatedResult());
    }

    async Task Because() => _result = await _grain.SeedWithResult(_entries);

    [Fact] void should_report_incomplete_seeding() => _result.ShouldEqual(SeedingResult.Incomplete);
    [Fact] void should_leave_every_entry_available_for_retry() => TrackedByEventType.ShouldBeEmpty();
}
