// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_ModelBoundCodeGenerator.given;

public class a_model_bound_code_generator : Specification
{
    protected ModelBoundCodeGenerator _generator = null!;
    protected ProjectionDefinition _definition = null!;
    protected ReadModelDefinition _readModelDefinition = null!;
    protected CompilationUnitSyntax _result = null!;

    void Establish()
    {
        _generator = new ModelBoundCodeGenerator();
    }

    protected ReadModelDefinition CreateReadModelDefinition(string name, Dictionary<string, JsonSchemaProperty> properties)
    {
        var schema = new JsonSchema
        {
            Title = name
        };

        foreach (var prop in properties)
        {
            schema.Properties[prop.Key] = prop.Value;
        }

        return new ReadModelDefinition(
            new ReadModelIdentifier(Guid.NewGuid().ToString()),
            new ReadModelContainerName(name),
            new ReadModelDisplayName(name),
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            new Concepts.Sinks.SinkDefinition(
                new Concepts.Sinks.SinkConfigurationId(Guid.NewGuid()),
                Concepts.Sinks.WellKnownSinkTypes.MongoDB),
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                [ReadModelGeneration.First] = schema
            },
            []);
    }

    protected EventType CreateEventType(string name) => new(new EventTypeId(name), new EventTypeGeneration(1), false);

    protected ProjectionDefinition CreateProjectionDefinition(
        IDictionary<EventType, FromDefinition>? from = null,
        IDictionary<EventType, JoinDefinition>? join = null,
        IDictionary<PropertyPath, ChildrenDefinition>? children = null) =>
        new(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            new ProjectionId("test-projection"),
            new ReadModelIdentifier("test-read-model"),
            true,
            true,
            new JsonObject(),
            from ?? new Dictionary<EventType, FromDefinition>(),
            join ?? new Dictionary<EventType, JoinDefinition>(),
            children ?? new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), true),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>());
}

