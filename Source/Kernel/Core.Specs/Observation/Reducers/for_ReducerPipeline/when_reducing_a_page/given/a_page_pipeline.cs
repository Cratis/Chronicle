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

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_reducing_a_page.given;

public class a_page_pipeline : Specification
{
    protected static readonly EventStoreName EventStore = "test-store";
    protected static readonly EventStoreNamespaceName EventStoreNamespace = "test-namespace";
    protected static readonly string[] Partitions = ["first", "second", "third"];

    protected PageSink _sink;
    protected ReducerPipeline _pipeline;
    protected List<IReadOnlyList<ReducerPagePartition>> _reducerCalls;
    protected Dictionary<string, ObserverSubscriberResult> _clientResults;
    protected ReducerPageOutcome _outcome;

    void Establish()
    {
        _sink = CreateSink();
        _reducerCalls = [];
        _clientResults = Partitions.ToDictionary(_ => _, _ => ObserverSubscriberResult.Ok(EventSequenceNumber.First));

        var objectComparer = Substitute.For<IObjectComparer>();
        objectComparer.Compare(Arg.Any<ExpandoObject>(), Arg.Any<ExpandoObject>(), out Arg.Any<IEnumerable<PropertyDifference>>())
            .Returns(callInfo =>
            {
                callInfo[2] = new[] { new PropertyDifference(new Properties.PropertyPath("value"), null, 1) };
                return false;
            });

        var readModelsCompliance = Substitute.For<IReadModelsCompliance>();
        readModelsCompliance.Apply(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<ExpandoObject>())
            .Returns(callInfo => Task.FromResult((ExpandoObject)callInfo[4]));
        readModelsCompliance.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<ExpandoObject>())
            .Returns(callInfo => Task.FromResult((ExpandoObject)callInfo[3]));

        _pipeline = new ReducerPipeline(
            new ReadModelDefinition(
                "test-read-model",
                "TestCollection",
                "Test Read Model",
                ReadModelOwner.Client,
                ReadModelSource.Code,
                ReadModelObserverType.Reducer,
                "test-observer",
                new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.InMemory),
                new Dictionary<ReadModelGeneration, JsonSchema> { { (ReadModelGeneration)1, new JsonSchema() } },
                []),
            _sink,
            objectComparer,
            readModelsCompliance,
            EventStore,
            EventStoreNamespace,
            memoryGate: CreateMemoryGate());
    }

    protected virtual PageSink CreateSink() => new();

    protected virtual ReducerMemoryGate? CreateMemoryGate() => null;

    protected static ExpandoObject StateWith(int value)
    {
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)["value"] = value;
        return state;
    }

    protected static IReadOnlyList<ReducerContext> Page() =>
        [.. Partitions.Select((partition, index) => new ReducerContext([CreateEvent(partition, (ulong)index)], new Key(partition, Properties.ArrayIndexers.NoIndexers)))];

    protected Task<IReadOnlyDictionary<Key, ReducerSubscriberResult>> Reduce(IReadOnlyList<ReducerPagePartition> partitions)
    {
        _reducerCalls.Add(partitions);
        IReadOnlyDictionary<Key, ReducerSubscriberResult> results = partitions.ToDictionary(
            _ => _.Key,
            _ => new ReducerSubscriberResult(_clientResults[_.Key.Value.ToString()!], StateWith(42)));
        return Task.FromResult(results);
    }

    protected async Task ReduceThePage() => _outcome = await _pipeline.ReducePage(Page(), Reduce);

    protected static AppendedEvent CreateEvent(string eventSourceId, ulong sequenceNumber)
    {
        var context = EventContext.From(
            EventStore,
            EventStoreNamespace,
            EventType.Unknown,
            EventSourceType.Default,
            eventSourceId,
            EventStreamType.All,
            EventStreamId.Default,
            sequenceNumber,
            CorrelationId.NotSet);
        return new AppendedEvent(context, new ExpandoObject());
    }
}
