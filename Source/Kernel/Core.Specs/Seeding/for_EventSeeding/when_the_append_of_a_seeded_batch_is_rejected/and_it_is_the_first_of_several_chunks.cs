// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_the_append_of_a_seeded_batch_is_rejected;

/// <summary>
/// A rejection in the first chunk must not cost the chunks behind it: they are unrelated events that share
/// nothing with the rejected one but a chunk index. The batch that was appended is recorded, the batch that
/// was not is left for the next run.
/// </summary>
public class and_it_is_the_first_of_several_chunks : given.an_event_seeding_grain
{
    const int NumberOfEntries = 150;

    IEnumerable<SeedingEntry> _entries;

    void Establish()
    {
        _entries = [.. Enumerable.Range(0, NumberOfEntries).Select(AnEntry)];
        AppendManyReturns(call => call == 0 ? AViolatedResult() : ASuccessfulResult());
    }

    async Task Because() => await _grain.Seed(_entries);

    [Fact] void should_record_only_the_entries_of_the_chunk_that_was_appended() => TrackedByEventType.Count().ShouldEqual(NumberOfEntries - 100);
    [Fact] void should_record_the_same_entries_by_event_source() => TrackedByEventSource.Count().ShouldEqual(NumberOfEntries - 100);
    [Fact] void should_not_record_any_entry_of_the_rejected_chunk() => TrackedByEventType.Select(_ => _.Content).ShouldNotContain(AnEntry(0).Content);
    [Fact] void should_record_the_last_entry_of_the_appended_chunk() => TrackedByEventType.Select(_ => _.Content).ShouldContain(AnEntry(NumberOfEntries - 1).Content);
    [Fact] void should_persist_what_was_appended() => _state.Received(1).WriteStateAsync();
}
