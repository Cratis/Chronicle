// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_seeding_repeated_identical_events;

/// <summary>
/// Declared control - green on both sides of the multiplicity fix, and the upper bound of the boundary the
/// sibling specs walk. Two byte-identical entries with nothing seeded yet are two facts, and both are
/// appended.
/// </summary>
public class and_none_were_seeded_before : given.an_event_seeding_grain
{
    IEnumerable<SeedingEntry> _entries;

    void Establish() => _entries = [
        new SeedingEntry("event-source-1", "test-event-type", /*lang=json,strict*/ "{\"value\":\"test1\"}", null),
        new SeedingEntry("event-source-1", "test-event-type", /*lang=json,strict*/ "{\"value\":\"test1\"}", null)
    ];

    async Task Because() => await _grain.Seed(_entries);

    [Fact]
    void should_append_both_occurrences() => _eventSequence.Received(1).AppendMany(
        Arg.Is<IEnumerable<EventToAppend>>(e => e.Count() == 2),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
        Arg.Any<Concepts.Identities.Identity>(),
        Arg.Any<Concepts.EventSequences.Concurrency.ConcurrencyScopes>());

    [Fact] void should_track_both_occurrences() => TrackedByEventType.Count().ShouldEqual(2);
}
