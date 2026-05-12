// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_Generator.given;

public class a_generator : Specification
{
    protected Generator _generator = null!;
    protected string _result = null!;

    void Establish()
    {
        _generator = new Generator();
    }

    protected ReadModelDefinition CreateReadModelDefinition(string identifier, string? schemaTitle)
    {
        var schema = new JsonSchema();
        if (schemaTitle is not null)
        {
            schema.Title = schemaTitle;
        }

        return new ReadModelDefinition(
            new ReadModelIdentifier(identifier),
            new ReadModelContainerName(identifier),
            new ReadModelDisplayName(identifier),
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

    protected ProjectionDefinition CreateProjectionDefinition(string projectionName, ReadModelIdentifier readModel) =>
        new(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            new ProjectionId(projectionName),
            readModel,
            true,
            true,
            new JsonObject(),
            new Dictionary<EventType, FromDefinition>(),
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<Properties.PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(new Dictionary<Properties.PropertyPath, string>(), true),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>());
}
