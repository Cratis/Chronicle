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

public class and_the_stored_state_carries_a_storage_id : given.all_dependencies
{
    ExpandoObject _stored;
    ReadModelChangeNotification _result;

    void Establish()
    {
        _stored = new ExpandoObject();
        var properties = (IDictionary<string, object?>)_stored;
        properties["_id"] = EventSourceIdValue;
        properties["value"] = 42;
        properties[WellKnownProperties.Subject] = EventSourceIdValue;
        _sink.FindOrDefault(Arg.Any<Key>()).Returns(_stored);
        _pipeline = new ReducerPipeline(
            _readModelDefinition,
            _sink,
            new ObjectComparer(),
            new ReadModelsCompliance(_complianceManager, _expandoObjectConverter),
            EventStore,
            EventStoreNamespace);
    }

    async Task Because()
    {
        var unchanged = new ExpandoObject();
        ((IDictionary<string, object?>)unchanged)["value"] = 42;
        _result = await _pipeline.Reduce(CreateContext(EventSourceIdValue), CreateReducer(unchanged));
    }

    [Fact] void should_not_report_a_change() => _result.ShouldBeNull();
    [Fact] void should_not_write_the_sink() => _sink.DidNotReceive().ApplyChanges(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<SinkWriteMode>());
    [Fact] void should_preserve_the_stored_id() => ((IDictionary<string, object?>)_stored)["_id"].ShouldEqual(EventSourceIdValue);
}
