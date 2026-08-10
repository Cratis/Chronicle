// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

/// <summary>
/// Declared control for the append-result fix - green on both sides of it. What it does pin is the chunking
/// the failure handling is expressed in terms of: a seed set larger than one batch is offered as several
/// appends, and every entry of every appended chunk is recorded.
/// </summary>
public class when_seeding_more_events_than_fit_in_one_batch : given.an_event_seeding_grain
{
    const int NumberOfEntries = 150;

    IEnumerable<SeedingEntry> _entries;

    void Establish() => _entries = [.. Enumerable.Range(0, NumberOfEntries).Select(AnEntry)];

    async Task Because() => await _grain.Seed(_entries);

    [Fact]
    void should_append_a_full_first_chunk() => _eventSequence.Received(1).AppendMany(
        Arg.Is<IEnumerable<EventToAppend>>(e => e.Count() == 100),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
        Arg.Any<Concepts.Identities.Identity>(),
        Arg.Any<Concepts.EventSequences.Concurrency.ConcurrencyScopes>());

    [Fact]
    void should_append_the_remainder_as_a_second_chunk() => _eventSequence.Received(1).AppendMany(
        Arg.Is<IEnumerable<EventToAppend>>(e => e.Count() == NumberOfEntries - 100),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
        Arg.Any<Concepts.Identities.Identity>(),
        Arg.Any<Concepts.EventSequences.Concurrency.ConcurrencyScopes>());

    [Fact] void should_record_every_entry_as_seeded() => TrackedByEventType.Count().ShouldEqual(NumberOfEntries);
    [Fact] void should_write_state_once() => _state.Received(1).WriteStateAsync();
}
