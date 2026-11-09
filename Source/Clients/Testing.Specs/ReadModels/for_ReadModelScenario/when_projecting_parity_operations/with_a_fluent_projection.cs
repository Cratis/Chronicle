// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_parity_operations;

/// <summary>
/// Set once, dictionary entries keyed by event content and ordered children run through the in-process engine
/// the same way they run in the kernel.
/// </summary>
public class with_a_fluent_projection : Specification
{
    ReadModelScenario<ParityBoard> _scenario;
    EventSourceId _id;
    Guid _first;
    Guid _second;
    Guid _third;

    void Establish()
    {
        _scenario = new ReadModelScenario<ParityBoard>();
        _id = new EventSourceId(Guid.NewGuid());
        _first = Guid.NewGuid();
        _second = Guid.NewGuid();
        _third = Guid.NewGuid();
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_id)
            .Events(
                new ParityBoardTitled("Draft"),
                new ParityBoardTitled("Final"),
                new ParitySlotAssigned("morning", "series-1"),
                new ParitySlotAssigned("evening", "series-2"),
                new ParitySlotAssigned("morning", "series-3"),
                new ParitySlotDropped("evening"),
                new ParitySlotDropped("never-assigned"),
                new ParityEntryAdded(_first, "First", 2),
                new ParityEntryAdded(_second, "Second", 3),
                new ParityEntryAdded(_third, "Third", 1),
                new ParityRankedEntryAdded(_first, "First", 2),
                new ParityRankedEntryAdded(_second, "Second", 3),
                new ParityRankedEntryAdded(_third, "Third", 1),
                new ParityEntryChanged(_third, "Third", 10));

    [Fact] void should_keep_the_first_title_set_once() => _scenario.Instance!.FirstTitle.ShouldEqual("Draft");
    [Fact] void should_keep_the_latest_title() => _scenario.Instance!.Title.ShouldEqual("Final");
    [Fact] void should_keep_only_the_slots_that_were_not_dropped() => _scenario.Instance!.Slots.Keys.ShouldContainOnly("morning");
    [Fact] void should_overwrite_a_slot_assigned_twice() => _scenario.Instance!.Slots["morning"].ShouldEqual("series-3");
    [Fact] void should_keep_prepended_entries_newest_first() => _scenario.Instance!.Entries.Select(_ => _.EntryId).ToArray().ShouldEqual([_third, _second, _first]);
    [Fact] void should_sort_ranked_entries_when_they_are_added() => _scenario.Instance!.RankedEntries.Select(_ => _.EntryId).ToArray().ShouldEqual([_second, _first, _third]);
    [Fact] void should_update_a_ranked_entry_in_place() => _scenario.Instance!.RankedEntries.Last().Rank.ShouldEqual(10);
}
