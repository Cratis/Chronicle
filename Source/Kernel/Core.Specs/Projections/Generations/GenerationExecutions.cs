// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.Chronicle.Storage.Sinks;
using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;

namespace Cratis.Chronicle.Projections.Generations;

public static class GenerationExecutions
{
    public const string Projection = "projection";
    public const string Namespace = "ns";

    public static MaterializationKey Key { get; } = new(new(Guid.Parse("3d1f0b4e-6f0c-4ab5-9a50-0a7d0f2c3e11")), Namespace, Projection, SinkTypeId.None, SinkConfigurationId.None);

    public static ProjectionDefinition Definition(string mappedFrom = "snapshotName") => new(
        ProjectionOwner.Client,
        EventSequenceId.Log,
        Projection,
        "model",
        true,
        true,
        new JsonObject(),
        new Dictionary<EventType, FromDefinition>(),
        new Dictionary<EventType, JoinDefinition>(),
        new Dictionary<PropertyPath, ChildrenDefinition>(),
        [],
        new FromEveryDefinition(new Dictionary<PropertyPath, string> { ["name"] = mappedFrom }, true),
        new Dictionary<EventType, RemovedWithDefinition>(),
        new Dictionary<EventType, RemovedWithJoinDefinition>())
    { Version = "1.2.3" };

    public static ReadModelDefinition ReadModel() => new(
        "model",
        "models",
        "model",
        ReadModelOwner.Client,
        ReadModelSource.Code,
        ReadModelObserverType.Projection,
        Projection,
        SinkDefinition.None,
        new Dictionary<ReadModelGeneration, JsonSchema> { [new(1)] = JsonSchema.FromJson("{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}") },
        []);

    public static EventTypeSchema[] EventSchemas() =>
    [
        new(new EventType("event", 1), EventTypeOwner.Client, EventTypeSource.Code, JsonSchema.FromJson("{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}"), EventTypeVisibility.Private, "origin")
    ];

    public static MaterializationGenerationIdentity Identity(uint generation) => MaterializationGenerationIdentity.For(Projection, generation, $"models-g{generation}");

    public static GenerationObserverBinding Binding(MaterializationExecutionSpecification execution, uint generation = 2) =>
        new(Key, Identity(generation), execution.EventSequence, execution.Revision);

    public static GenerationExecution Create(EngineProjection projection, uint generation = 2)
    {
        var execution = MaterializationExecutionSpecification.Capture(EventSequenceId.Log, "{\"generation\":" + generation + "}");
        var inputs = new MaterializationExecutionInputs(Definition(), ReadModel(), EventSchemas(), new("sink-v1"));
        return new(Binding(execution, generation), execution, inputs, projection);
    }

    public static GenerationSinkContext Context(GenerationExecution execution, long epoch = 1) =>
        new(execution.Binding, execution.Execution, new GenerationWriterGrant("store", execution.Binding, new(Guid.Parse("8c2f7e0b-1d3a-4f61-9e0d-5b7a2c4e6f80")), epoch));
}
