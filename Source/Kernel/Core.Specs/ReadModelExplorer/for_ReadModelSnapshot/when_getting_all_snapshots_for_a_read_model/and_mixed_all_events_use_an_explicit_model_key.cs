// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.ReadModelExplorer.for_ReadModelSnapshot.when_getting_all_snapshots_for_a_read_model;

public class and_mixed_all_events_use_an_explicit_model_key : given.a_mixed_all_event_history
{
    IEnumerable<ReadModelSnapshot> _result;

    void Establish() => _definition = _definition with
    {
        From = new Dictionary<EventType, FromDefinition> { [new("A", EventTypeGeneration.First)] = new(new Dictionary<PropertyPath, string>(), new PropertyExpression("modelKey"), null) }
    };

    async Task Because() => _result = await AllSnapshots(nameof(ReadModelSnapshotGrouping.Event));

    [Fact] void should_include_every_event_in_history() => _result.SelectMany(snapshot => snapshot.Events).Select(@event => @event.Context.SequenceNumber).ShouldEqual<IEnumerable<ulong>>([1, 2, 3]);
}
