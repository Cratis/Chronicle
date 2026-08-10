// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_the_append_of_a_seeded_batch_is_rejected;

/// <summary>
/// Not recording a rejected batch is only worth anything if the next run picks it up again. This is the
/// property the whole fix exists for: the second run re-offers every entry of the rejected chunk - the one
/// that caused the rejection and the innocent ones that merely shared its chunk - and records them once
/// they are actually appended.
/// </summary>
public class and_seeding_runs_again : given.an_event_seeding_grain
{
    IEnumerable<SeedingEntry> _entries;
    int _appendCount;

    void Establish()
    {
        _entries = [AnEntry(1), AnEntry(2), AnEntry(3)];
        AppendManyReturns(call =>
        {
            _appendCount++;
            return call == 0 ? AViolatedResult() : ASuccessfulResult();
        });
    }

    async Task Because()
    {
        await _grain.Seed(_entries);
        await _grain.Seed(_entries);
    }

    [Fact] void should_offer_the_rejected_entries_to_the_event_sequence_again() => _appendCount.ShouldEqual(2);

    [Fact]
    void should_offer_every_entry_of_the_rejected_chunk_again() => _eventSequence.Received(2).AppendMany(
        Arg.Is<IEnumerable<EventToAppend>>(e => e.Count() == 3),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
        Arg.Any<Concepts.Identities.Identity>(),
        Arg.Any<Concepts.EventSequences.Concurrency.ConcurrencyScopes>());

    [Fact] void should_record_every_entry_once_it_has_been_appended() => TrackedByEventType.Count().ShouldEqual(3);
    [Fact] void should_persist_only_after_the_successful_run() => _state.Received(1).WriteStateAsync();
}
