// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reducers;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink;

public class when_reducing_the_same_key_twice : Specification
{
    SqlSinkHarness _harness;
    ISink _sink;
    ReducerPipeline _pipeline;
    Key _key;
    IDictionary<string, object?> _stored;
    JsonObject _clientRead;
    JsonSchema _schema;

    void Establish()
    {
        _harness = new SqlSinkHarness();
        _key = new Key("counter-1", ArrayIndexers.NoIndexers);
        _schema = JsonSchema.FromJson("""
            {"type":"object","properties":{"id":{"type":"string"},"count":{"type":"integer"},"note":{"type":["string","null"]},"score":{"type":["integer","null"]}}}
            """);
        var definition = new ReadModelDefinition(
            "counter",
            "counters",
            "Counter",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Reducer,
            "counter-reducer",
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = _schema },
            []);
        _sink = _harness.CreateSink(definition);
        _pipeline = new ReducerPipeline(
            definition,
            _sink,
            new ObjectComparer(),
            new ReadModelsCompliance(Substitute.For<IJsonSchemaMetadataManager>(), new ExpandoObjectConverter(new TypeFormats())),
            "store",
            "namespace");
    }

    async Task Because()
    {
        await _pipeline.Reduce(Context(0UL), Reduce);
        await _pipeline.Reduce(Context(1UL), Reduce);
        _stored = (await _harness.ReadStoredRows()).Single();
        _clientRead = new ExpandoObjectConverter(new TypeFormats()).ToJsonObject((await _sink.FindOrDefault(_key))!, _schema);
    }

    void Destroy() => _harness.Dispose();

    [Fact] void should_stay_initialized() => _stored[WellKnownProperties.ReadModelInstanceInitialized].ShouldEqual(true);
    [Fact] void should_accumulate_both_events() => _clientRead["count"]!.GetValue<int>().ShouldEqual(2);
    [Fact] void should_preserve_the_null_column() => _stored["note"].ShouldBeNull();
    [Fact] void should_not_replace_a_null_number_with_zero() => _stored["score"].ShouldBeNull();
    [Fact] void should_leave_the_nullable_string_unset_in_client_reads() => _clientRead.ContainsKey("note").ShouldBeFalse();
    [Fact] void should_leave_the_nullable_number_unset_in_client_reads() => _clientRead.ContainsKey("score").ShouldBeFalse();
    [Fact] void should_not_expose_initialization_in_client_reads() => _clientRead.ContainsKey(WellKnownProperties.ReadModelInstanceInitialized).ShouldBeFalse();

    ReducerContext Context(EventSequenceNumber sequenceNumber) => new(
        [new AppendedEvent(EventContext.From("store", "namespace", EventType.Unknown, EventSourceType.Default, "counter-1", EventStreamType.All, EventStreamId.Default, sequenceNumber, CorrelationId.NotSet), new ExpandoObject())], _key);

    static Task<ReducerSubscriberResult> Reduce(IEnumerable<AppendedEvent> events, ExpandoObject? initial)
    {
        dynamic state = new ExpandoObject();
        state.id = "counter-1";
        state.count = initial is null ? 1 : (int)((IDictionary<string, object?>)initial)["count"]! + 1;
        state.note = null;
        state.score = null;
        return Task.FromResult(new ReducerSubscriberResult(new ObserverSubscriberResult(ObserverSubscriberState.Ok, events.Last().Context.SequenceNumber, [], string.Empty), state));
    }
}
