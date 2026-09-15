// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.CSharp;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.for_CSharpProjectionCodeGenerator.when_generating_declarative;

public class with_from_all_count : Specification
{
    CSharpProjectionCodeGenerator _generator = null!;
    ProjectionDefinition _definition = null!;
    ReadModelDefinition _readModelDefinition = null!;
    string _result = null!;

    void Establish()
    {
        _generator = new CSharpProjectionCodeGenerator(new DeclarativeCodeGenerator(), new ModelBoundCodeGenerator());

        _readModelDefinition = new ReadModelDefinition(
            new ReadModelIdentifier(Guid.NewGuid().ToString()),
            new ReadModelContainerName("EventStats"),
            new ReadModelDisplayName("EventStats"),
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            new Cratis.Chronicle.Concepts.Sinks.SinkDefinition(
                new Cratis.Chronicle.Concepts.Sinks.SinkConfigurationId(Guid.NewGuid()),
                Cratis.Chronicle.Concepts.Sinks.WellKnownSinkTypes.MongoDB),
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = CreateSchema() },
            []);

        var fromEvery = new FromEveryDefinition(
            new Dictionary<PropertyPath, string>
            {
                [new PropertyPath("TotalEventCount")] = "count"
            },
            true);

        _definition = new ProjectionDefinition(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            new ProjectionId("EventStatsProjection"),
            _readModelDefinition.Identifier,
            true,
            true,
            new JsonObject(),
            new Dictionary<EventType, FromDefinition>(),
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            fromEvery,
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            SubscribesToAllEvents: true);
    }

    void Because() => _result = _generator.GenerateDeclarative(_definition, _readModelDefinition);

    [Fact] void should_have_from_all_method() => _result.ShouldContain(".FromAll");
    [Fact] void should_have_count_mapping() => _result.ShouldContain(".Count(m => m.TotalEventCount)");

    static JsonSchema CreateSchema()
    {
        var schema = new JsonSchema { Title = "EventStats" };
        schema.Properties["Id"] = new JsonSchemaProperty { Type = JsonObjectType.String };
        schema.Properties["TotalEventCount"] = new JsonSchemaProperty { Type = JsonObjectType.Integer };
        return schema;
    }
}
#endif
