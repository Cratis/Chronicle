// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_seeding_already_seeded_events : given.an_event_seeding_grain
{
    IEnumerable<SeedingEntry> _entries;

    void Establish()
    {
        // Add an entry to the state to simulate it was already seeded
        var seededEntry = new Storage.Seeding.SeededEventEntry("event-source-1", "test-event-type", /*lang=json,strict*/ "{\"value\":\"test1\"}", null);
        _state.State.ByEventType["test-event-type"] = [seededEntry];

        _entries = [
            new SeedingEntry("event-source-1", "test-event-type", /*lang=json,strict*/ "{\"value\":\"test1\"}", null)
        ];
    }

    async Task Because() => await _grain.Seed(_entries);

    [Fact]
    void should_not_append_events() => _eventSequence.DidNotReceive().AppendMany(
        Arg.Any<IEnumerable<EventToAppend>>(),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
        Arg.Any<Concepts.Identities.Identity>(),
        Arg.Any<Concepts.EventSequences.Concurrency.ConcurrencyScopes>());

    [Fact] void should_not_write_state() => _state.DidNotReceive().WriteStateAsync();
}
