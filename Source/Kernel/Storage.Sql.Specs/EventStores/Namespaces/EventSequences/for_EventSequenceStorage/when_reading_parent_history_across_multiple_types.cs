// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage;

/// <summary>
/// Per-type predecessor seeks must merge into one latest predecessor for each source.
/// </summary>
public class when_reading_parent_history_across_multiple_types : given.a_storage_for_appended_generation
{
    IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>> _window;
    IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>> _predecessors;

    async Task Establish() => (await _storage.AppendMany(
    [
        EventAt(0, 1),
        EventAt(1, 1) with { EventType = new("second-parent", 1) },
        EventAt(2, 1) with { EventType = new("second-parent", 1), EventSourceId = "other-source" },
        EventAt(3, 1) with { EventSourceId = "other-source" },
        EventAt(4, 1),
        EventAt(5, 1) with { EventType = new("second-parent", 1) },
        EventAt(6, 1) with { EventType = new("excluded", 1) }
    ])).IsSuccess.ShouldBeTrue();

    async Task Because()
    {
        _window = await _storage.GetInstancesOfAnyInRange(["some-source", "other-source", "missing"], [_eventType.Id, "second-parent"], 4, 6);
        _predecessors = await _storage.GetInstancesOfAnyInRange(["some-source", "other-source"], [_eventType.Id, "second-parent"], 4, 3);
    }

    [Fact] void should_choose_the_latest_predecessor_across_types_before_the_window() => _window["some-source"].Select(entry => entry.Context.SequenceNumber.Value).ShouldEqual([1UL, 4UL, 5UL]);
    [Fact] void should_choose_a_different_type_for_another_sources_latest_predecessor() => _window["other-source"].Single().Context.SequenceNumber.Value.ShouldEqual(3UL);
    [Fact] void should_return_only_one_predecessor_per_source_when_the_window_is_empty() => _predecessors.Values.Select(entries => entries.Single().Context.SequenceNumber.Value).Order().ShouldEqual([1UL, 3UL]);
    [Fact] void should_omit_missing_sources() => _window.Count.ShouldEqual(2);
}
