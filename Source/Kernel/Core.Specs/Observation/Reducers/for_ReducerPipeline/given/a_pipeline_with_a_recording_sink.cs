// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.given;

public class a_pipeline_with_a_recording_sink : Specification
{
    protected static readonly EventStoreName EventStore = "test-store";
    protected static readonly EventStoreNamespaceName EventStoreNamespace = "test-namespace";
    protected const string EventSourceIdValue = "event-source-id";

    protected RecordingSink _sink;
    protected ReducerPipeline _pipeline;

    /// <summary>
    /// Gets a value indicating whether the pipeline is built with the watermark guard enabled.
    /// </summary>
    protected virtual bool GuardWritesOnWatermark => true;

    void Establish()
    {
        _sink = new RecordingSink();

        var objectComparer = Substitute.For<IObjectComparer>();
        objectComparer.Compare(Arg.Any<ExpandoObject>(), Arg.Any<ExpandoObject>(), out Arg.Any<IEnumerable<PropertyDifference>>())
            .Returns(callInfo =>
            {
                callInfo[2] = new[] { new PropertyDifference(new Properties.PropertyPath("value"), null, 1) };
                return false;
            });

        var readModelsCompliance = Substitute.For<IReadModelsCompliance>();
        readModelsCompliance.Apply(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<ExpandoObject>())
            .Returns(callInfo => Task.FromResult<ExpandoObject>((ExpandoObject)callInfo[4]));
        readModelsCompliance.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<ExpandoObject>())
            .Returns(callInfo => Task.FromResult<ExpandoObject>((ExpandoObject)callInfo[3]));

        _pipeline = new ReducerPipeline(
            CreateReadModelDefinition(),
            _sink,
            objectComparer,
            readModelsCompliance,
            EventStore,
            EventStoreNamespace,
            GuardWritesOnWatermark);
    }

    protected static ExpandoObject NewState()
    {
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)["value"] = 1;
        return state;
    }

    protected static ReducerContext CreateContext() =>
        new([CreateEvent()], new Key(EventSourceIdValue, Properties.ArrayIndexers.NoIndexers));

    protected static ReducerDelegate CreateReducer(ExpandoObject? returnState) =>
        (_, _) => Task.FromResult(new ReducerSubscriberResult(
            new ObserverSubscriberResult(ObserverSubscriberState.Ok, EventSequenceNumber.First, [], string.Empty),
            returnState));

    static AppendedEvent CreateEvent()
    {
        var context = EventContext.From(
            EventStore,
            EventStoreNamespace,
            EventType.Unknown,
            EventSourceType.Default,
            EventSourceIdValue,
            EventStreamType.All,
            EventStreamId.Default,
            EventSequenceNumber.First,
            CorrelationId.NotSet);
        return new AppendedEvent(context, new ExpandoObject());
    }

    static ReadModelDefinition CreateReadModelDefinition() =>
        new(
            "test-read-model",
            "TestCollection",
            "Test Read Model",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Reducer,
            "test-observer",
            new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.MongoDB),
            new Dictionary<ReadModelGeneration, JsonSchema> { { (ReadModelGeneration)1, new JsonSchema() } },
            []);
}
