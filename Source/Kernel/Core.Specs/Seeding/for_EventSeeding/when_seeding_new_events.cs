// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

/// <summary>
/// The happy path, and the control the rejected-batch specs are read against: when the event sequence
/// reports that it appended the batch, every entry of it is recorded as seeded and the claim is persisted.
/// The two tracking facts are green on both sides of the append-result fix by design - they are what makes
/// "not recorded" mean something in the specs that assert it.
/// </summary>
public class when_seeding_new_events : given.an_event_seeding_grain
{
    IEnumerable<SeedingEntry> _entries;

    void Establish()
    {
        _entries = [
            new SeedingEntry("event-source-1", "test-event-type", /*lang=json,strict*/ "{\"value\":\"test1\"}", null),
            new SeedingEntry("event-source-2", "test-event-type", /*lang=json,strict*/ "{\"value\":\"test2\"}", null)
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

    [Fact] void should_record_every_entry_as_seeded_by_event_type() => TrackedByEventType.Count().ShouldEqual(2);
    [Fact] void should_record_every_entry_as_seeded_by_event_source() => TrackedByEventSource.Count().ShouldEqual(2);
    [Fact] void should_write_state() => _state.Received(1).WriteStateAsync();
}
