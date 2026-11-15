// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Concepts.Projections.Graphs;
using Cratis.Chronicle.Projections.Engine.Graphs;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Graphs;
using Cratis.Chronicle.Storage.InMemory.Sinks.Graphs;
using Microsoft.Extensions.Logging.Abstractions;
using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;

namespace Cratis.Chronicle.Storage.InMemory.Projections.Graphs.for_GraphProjectionPipeline.when_handling_events;

public class and_an_event_carries_a_collection : given.a_graph_projection
{
    static readonly EventType _modelDefined = new("ModelDefined", EventTypeGeneration.First);
    static readonly EventType _modelSlotsCleared = new("ModelSlotsCleared", EventTypeGeneration.First);

    InMemoryGraphSink _scoring;
    GraphProjectionPipeline _scoringPipeline;
    GraphEdge? _first;
    GraphEdge? _second;
    GraphEdge? _afterClear;
    GraphEdge? _withoutCollection;

    static GraphDefinition Scoring => new(
        "Scoring",
        [
            new("Model", "modelId", Schema(("modelId", JsonObjectType.String))),
            new("Series", "seriesId", Schema(("seriesId", JsonObjectType.String)))
        ],
        [
            new("SCORES", "Model", "Series", "slot", Schema(("slot", JsonObjectType.String)))
        ]);

    static GraphProjectionDefinition ScoringDefinition => new(
        new Dictionary<string, GraphNodeProjectionDefinition>
        {
            ["Model"] = new(
                new Dictionary<string, GraphNodeFromDefinition> { [_modelDefined.ToString()] = new("modelId", new Dictionary<string, string>()) },
                new Dictionary<string, GraphNodeRemovedWithDefinition>())
        },
        new Dictionary<string, GraphEdgeProjectionDefinition>
        {
            ["SCORES"] = new(
                new Dictionary<string, GraphEdgeFromDefinition>
                {
                    [_modelDefined.ToString()] = new("modelId", "slots.series", "slots.slot", new Dictionary<string, string>(), "slots")
                },
                new Dictionary<string, GraphEdgeRemovedWithDefinition>
                {
                    [_modelSlotsCleared.ToString()] = new("modelId", "slots.series", "slots.slot", "slots")
                })
        });

    static JsonSchema Schema(params (string Name, JsonObjectType Type)[] properties)
    {
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        foreach (var (name, type) in properties) schema.Properties[name] = new JsonSchemaProperty { Type = type };
        return schema;
    }

    static ExpandoObject Slot(string slot, string series)
    {
        var item = new ExpandoObject();
        ((IDictionary<string, object?>)item)["slot"] = slot;
        ((IDictionary<string, object?>)item)["series"] = series;
        return item;
    }

    async Task Because()
    {
        _scoring = new InMemoryGraphSink(Scoring);
        var projection = Substitute.For<EngineProjection>();
        projection.Identifier.Returns(_projectionId);
        _scoringPipeline = new GraphProjectionPipeline(
            projection,
            new GraphChangesetBuilder(CreatePlan(ScoringDefinition, Scoring), _scoring, _projectionId),
            _scoring,
            new ProjectionHandleLock(),
            new ObjectComparer(),
            NullLogger<GraphProjectionPipeline>.Instance);

        var slots = new List<object> { Slot("fuel", "s1"), Slot("speed", "s2") };
        await _scoringPipeline.Handle(Event(_modelDefined, 1, "m1", ("modelId", "m1"), ("slots", slots)));
        _first = await Edge("m1", "s1", "fuel");
        _second = await Edge("m1", "s2", "speed");

        await _scoringPipeline.Handle(Event(_modelDefined, 2, "m2", ("modelId", "m2")));
        _withoutCollection = await Edge("m2", "s1", "fuel");

        await _scoringPipeline.Handle(Event(_modelSlotsCleared, 3, "m1", ("modelId", "m1"), ("slots", new List<object> { Slot("fuel", "s1") })));
        _afterClear = await Edge("m1", "s1", "fuel");
    }

    Task<GraphEdge?> Edge(string model, string series, string slot) =>
        _scoring.FindEdge(_projectionId, new EdgeReference("SCORES", new("Model", model), new("Series", series), slot));

    [Fact] void should_create_an_edge_for_the_first_item() => _first.ShouldNotBeNull();
    [Fact] void should_create_an_edge_for_the_second_item() => _second.ShouldNotBeNull();
    [Fact] void should_carry_the_item_identity() => _second!.Properties["slot"].ShouldEqual("speed");
    [Fact] void should_do_nothing_when_the_collection_is_absent() => _withoutCollection.ShouldBeNull();
    [Fact] void should_remove_only_the_edges_of_the_items_listed() => _afterClear.ShouldBeNull();
}
