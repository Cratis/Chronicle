// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_seeding_repeated_identical_events;

/// <summary>
/// The seeded tracking used to answer "is there an equal entry?" rather than "how many of these have been
/// seeded?", so a fact that really happened twice was skipped entirely once one of the two had landed. That
/// is reachable exactly when a batch is rejected or a chunk boundary falls between the two, which is the
/// same moment the retry is supposed to repair - the guard would have quietly cancelled the repair.
/// </summary>
public class and_only_one_was_seeded_before : given.an_event_seeding_grain
{
    IEnumerable<SeedingEntry> _entries;

    void Establish()
    {
        var seededEntry = new Storage.Seeding.SeededEventEntry("event-source-1", "test-event-type", /*lang=json,strict*/ "{\"value\":\"test1\"}", []);
        _state.State.ByEventType["test-event-type"] = [seededEntry];
        _state.State.ByEventSource["event-source-1"] = [seededEntry];

        _entries = [
            new SeedingEntry("event-source-1", "test-event-type", /*lang=json,strict*/ "{\"value\":\"test1\"}", null),
            new SeedingEntry("event-source-1", "test-event-type", /*lang=json,strict*/ "{\"value\":\"test1\"}", null)
        ];
    }

    async Task Because() => await _grain.Seed(_entries);

    [Fact]
    void should_append_only_the_occurrence_that_is_still_missing() => _eventSequence.Received(1).AppendMany(
        Arg.Is<IEnumerable<EventToAppend>>(e => e.Count() == 1),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
        Arg.Any<Concepts.Identities.Identity>(),
        Arg.Any<Concepts.EventSequences.Concurrency.ConcurrencyScopes>());

    [Fact] void should_end_up_tracking_both_occurrences() => TrackedByEventType.Count().ShouldEqual(2);
}
