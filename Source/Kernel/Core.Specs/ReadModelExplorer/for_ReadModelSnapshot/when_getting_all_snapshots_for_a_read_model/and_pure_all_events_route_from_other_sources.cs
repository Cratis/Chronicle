// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.ReadModelExplorer.for_ReadModelSnapshot.when_getting_all_snapshots_for_a_read_model;

public class and_pure_all_events_route_from_other_sources : given.a_custom_all_event_key_history
{
    IEnumerable<ReadModelSnapshot> _result;

    void Establish()
    {
        _definition = _definition with { From = new Dictionary<EventType, FromDefinition>() };
        _projection.GetEventTypes().Returns([]);
    }

    async Task Because() => _result = await AllSnapshots(nameof(ReadModelSnapshotGrouping.Event));

    [Fact] void should_include_the_event_routed_by_the_all_event_key() => _result.SelectMany(snapshot => snapshot.Events).Select(@event => @event.Context.SequenceNumber).ShouldEqual<IEnumerable<ulong>>([1, 2, 3]);
    [Fact] async Task should_resolve_the_instance_key_after_reading() => await _projection.Received(1).GetEventsForKey("test-namespace", "my-instance", Arg.Any<IEnumerable<AppendedEvent>>());
}
