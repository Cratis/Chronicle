// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_handling;

public class and_the_schema_declares_bookkeeping_properties : given.all_dependencies
{
    ExpandoObject _changed;
    PropertyDifference[] _differences;

    void Establish()
    {
        _schema.Properties.Add(WellKnownProperties.ReadModelInstanceInitialized, new JsonSchemaProperty(WellKnownProperties.ReadModelInstanceInitialized, new JsonObject { ["type"] = "boolean" }, _schema));
        _schema.Properties.Add(WellKnownProperties.LastHandledEventSequenceNumber, new JsonSchemaProperty(WellKnownProperties.LastHandledEventSequenceNumber, new JsonObject { ["type"] = "integer" }, _schema));
        var initial = new ExpandoObject();
        ((IDictionary<string, object?>)initial)[WellKnownProperties.ReadModelInstanceInitialized] = false;
        ((IDictionary<string, object?>)initial)[WellKnownProperties.LastHandledEventSequenceNumber] = 1UL;
        _changed = new ExpandoObject();
        ((IDictionary<string, object?>)_changed)[WellKnownProperties.ReadModelInstanceInitialized] = true;
        ((IDictionary<string, object?>)_changed)[WellKnownProperties.LastHandledEventSequenceNumber] = 2UL;
        _sink.FindOrDefault(Arg.Any<Concepts.Keys.Key>()).Returns(initial);
        _sink.ApplyChanges(Arg.Any<Concepts.Keys.Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<SinkWriteMode>())
            .Returns(call =>
            {
                _differences = call.ArgAt<IChangeset<AppendedEvent, ExpandoObject>>(1).Changes
                    .OfType<PropertiesChanged<ExpandoObject>>().SelectMany(change => change.Differences).ToArray();
                return Task.FromResult(Enumerable.Empty<FailedPartition>());
            });
        _pipeline = new ReducerPipeline(_readModelDefinition, _sink, new ObjectComparer(), new ReadModelsCompliance(_complianceManager, _expandoObjectConverter), EventStore, EventStoreNamespace);
    }

    Task Because() => _pipeline.Reduce(CreateContext(EventSourceIdValue), CreateReducer(_changed));

    [Fact] void should_compare_the_declared_initialization_property() => _differences.Single(difference => difference.PropertyPath.Path == WellKnownProperties.ReadModelInstanceInitialized).Original.ShouldEqual(false);
    [Fact] void should_compare_the_declared_watermark_property() => _differences.Single(difference => difference.PropertyPath.Path == WellKnownProperties.LastHandledEventSequenceNumber).Original.ShouldEqual(1UL);
}
