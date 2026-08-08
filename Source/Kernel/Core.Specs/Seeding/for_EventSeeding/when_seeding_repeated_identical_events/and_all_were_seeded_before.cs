// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_seeding_repeated_identical_events;

/// <summary>
/// Declared control - green on both sides of the multiplicity fix, and the lower bound of the boundary.
/// Counting occurrences instead of testing for existence must not cost idempotency: a re-seed of a set
/// whose repeats have all already landed appends nothing at all.
/// </summary>
public class and_all_were_seeded_before : given.an_event_seeding_grain
{
    IEnumerable<SeedingEntry> _entries;

    void Establish()
    {
        var seededEntry = new Storage.Seeding.SeededEventEntry("event-source-1", "test-event-type", /*lang=json,strict*/ "{\"value\":\"test1\"}", []);
        _state.State.ByEventType["test-event-type"] = [seededEntry, seededEntry];
        _state.State.ByEventSource["event-source-1"] = [seededEntry, seededEntry];

        _entries = [
            new SeedingEntry("event-source-1", "test-event-type", /*lang=json,strict*/ "{\"value\":\"test1\"}", null),
            new SeedingEntry("event-source-1", "test-event-type", /*lang=json,strict*/ "{\"value\":\"test1\"}", null)
        ];
    }

    async Task Because() => await _grain.Seed(_entries);

    [Fact]
    void should_not_append_anything() => _eventSequence.DidNotReceive().AppendMany(
        Arg.Any<IEnumerable<EventToAppend>>(),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
        Arg.Any<Concepts.Identities.Identity>(),
        Arg.Any<Concepts.EventSequences.Concurrency.ConcurrencyScopes>());

    [Fact] void should_not_write_state() => _state.DidNotReceive().WriteStateAsync();
}
