// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_reducing;

public class and_unchanged_state_contains_a_sink_identity : given.all_dependencies
{
    ExpandoObject _initial;
    KeyValuePair<string, object?>[] _initialSnapshot;
    ReadModelChangeNotification _result;

    void Establish()
    {
        _initial = new ExpandoObject();
        var values = (IDictionary<string, object?>)_initial;
        values["_id"] = EventSourceIdValue;
        values["value"] = 1;
        values[WellKnownProperties.ReadModelInstanceInitialized] = true;
        values[WellKnownProperties.LastHandledEventSequenceNumber] = 1UL;
        values[WellKnownProperties.Subject] = EventSourceIdValue;
        _initialSnapshot = values.ToArray();
        _sink.FindOrDefault(Arg.Any<Key>()).Returns(_initial);
        _pipeline = new ReducerPipeline(_readModelDefinition, _sink, new ObjectComparer(), new ReadModelsCompliance(_complianceManager, _expandoObjectConverter), EventStore, EventStoreNamespace);
    }

    async Task Because()
    {
        dynamic state = new ExpandoObject();
        state.value = 1;
        _result = await _pipeline.Reduce(CreateContext(EventSourceIdValue), CreateReducer(state));
    }

    [Fact] void should_not_report_a_change() => _result.ShouldBeNull();
    [Fact] void should_not_write_the_sink() => _sink.DidNotReceive().ApplyChanges(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<SinkWriteMode>());
    [Fact] void should_not_mutate_the_stored_initial_state() => _initial.ShouldContainOnly(_initialSnapshot);
}
