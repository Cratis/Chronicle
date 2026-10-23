// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage;

public class when_reading_a_bounded_parent_history : given.a_storage_for_appended_generation
{
    IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>> _result;
    IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>> _predecessor;
    IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>> _singleSourceWindow;
    IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>> _emptySources;
    IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>> _emptyTypes;

    async Task Establish()
    {
        var history = Enumerable.Range(0, 2000).Select(i => EventAt((ulong)i, i % 2 == 0 ? 1U : 2U));
        (await _storage.AppendMany(history.Concat(
        [
            EventAt(2000, 1) with { EventType = new EventType("excluded", 1) },
            EventAt(2001, 1) with { EventSourceId = "other-source" }
        ]))).IsSuccess.ShouldBeTrue();
    }

    async Task Because()
    {
        _result = await _storage.GetInstancesOfAnyInRange(["some-source", "some-source", "other-source", "missing"], [_eventType.Id], 1997, 2001);
        _predecessor = await _storage.GetInstancesOfAnyInRange(["some-source"], [_eventType.Id], 1997, 1996);
        _singleSourceWindow = await _storage.GetInstancesOfAnyInRange(["some-source"], [_eventType.Id], 1997, 2001);
        _emptySources = await _storage.GetInstancesOfAnyInRange([], [_eventType.Id], 0, 2001);
        _emptyTypes = await _storage.GetInstancesOfAnyInRange(["some-source"], [], 0, 2001);
    }

    [Fact] void should_return_only_the_latest_predecessor_and_the_inclusive_window() => _result["some-source"].Select(e => e.Context.SequenceNumber.Value).ShouldEqual([1996UL, 1997UL, 1998UL, 1999UL]);
    [Fact] void should_match_across_generations() => _result["some-source"].Select(e => e.Context.AppendedGeneration!.Value).ShouldEqual([1U, 2U, 1U, 2U]);
    [Fact] void should_include_the_upper_boundary_for_the_other_source() => _result["other-source"].Single().Context.SequenceNumber.Value.ShouldEqual(2001UL);
    [Fact] void should_omit_missing_sources_and_deduplicate_requested_sources() => _result.Count.ShouldEqual(2);
    [Fact] void should_support_a_single_predecessor_lookup_without_loading_the_history() => _predecessor["some-source"].Single().Context.SequenceNumber.Value.ShouldEqual(1996UL);
    [Fact] void should_combine_the_single_source_predecessor_and_window() => _singleSourceWindow["some-source"].Select(e => e.Context.SequenceNumber.Value).ShouldEqual([1996UL, 1997UL, 1998UL, 1999UL]);
    [Fact] void should_return_no_matches_for_no_sources() => _emptySources.Count.ShouldEqual(0);
    [Fact] void should_return_no_matches_for_no_types() => _emptyTypes.Count.ShouldEqual(0);
}
