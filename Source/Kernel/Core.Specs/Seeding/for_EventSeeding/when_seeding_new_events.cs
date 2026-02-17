// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_seeding_new_events : given.an_event_seeding_grain
{
    IEnumerable<SeedingEntry> _entries;

    void Establish()
    {
        _entries = [
            new SeedingEntry("event-source-1", "test-event-type", /*lang=json,strict*/ "{\"value\":\"test1\"}"),
            new SeedingEntry("event-source-2", "test-event-type", /*lang=json,strict*/ "{\"value\":\"test2\"}")
        ];
    }

    async Task Because() => await _grain.Seed(_entries);

    [Fact]
    void should_append_events() => _eventSequence.Received(1).AppendMany(
        Arg.Is<IEnumerable<EventToAppend>>(e => e.Count() == 2),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
        Arg.Any<Concepts.Identities.Identity>(),
        Arg.Any<Concepts.EventSequences.Concurrency.ConcurrencyScopes>());

    [Fact] void should_write_state() => _state.Received(1).WriteStateAsync();
}
