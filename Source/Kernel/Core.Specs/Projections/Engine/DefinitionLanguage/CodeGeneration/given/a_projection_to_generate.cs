// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.given;

/// <summary>
/// One projection that every language generator is asked to render, so the specs compare the same
/// shape across languages rather than each inventing its own.
/// </summary>
public class a_projection_to_generate : Specification
{
    protected ProjectionDefinition _definition = null!;
    protected ReadModelDefinition _readModelDefinition = null!;
    protected string _result = null!;

    void Establish()
    {
        _readModelDefinition = CreateReadModelDefinition("Employee", new()
        {
            ["Id"] = new JsonSchemaProperty { Type = JsonObjectType.String },
            ["FirstName"] = new JsonSchemaProperty { Type = JsonObjectType.String },
            ["Title"] = new JsonSchemaProperty { Type = JsonObjectType.String },
            ["PromotionCount"] = new JsonSchemaProperty { Type = JsonObjectType.Integer }
        });

        var from = new Dictionary<EventType, FromDefinition>
        {
            [CreateEventType("EmployeeHired")] = new FromDefinition(
                new Dictionary<PropertyPath, string>
                {
                    [new PropertyPath("FirstName")] = "FirstName"
                },
                new PropertyExpression(WellKnownExpressions.EventSourceId),
                null),
            [CreateEventType("EmployeePromoted")] = new FromDefinition(
                new Dictionary<PropertyPath, string>
                {
                    [new PropertyPath("Title")] = "NewTitle",
                    [new PropertyPath("PromotionCount")] = "count"
                },
                new PropertyExpression(WellKnownExpressions.EventSourceId),
                null)
        };

        _definition = CreateProjectionDefinition("EmployeeProjection", _readModelDefinition.Identifier, from);
    }

    protected static ReadModelDefinition CreateReadModelDefinition(string name, Dictionary<string, JsonSchemaProperty> properties)
    {
        var schema = new JsonSchema { Title = name };
        foreach (var property in properties)
        {
            schema.Properties[property.Key] = property.Value;
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
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema },
            []);
    }

    protected static EventType CreateEventType(string name) => new(new EventTypeId(name), new EventTypeGeneration(1), false);

    protected static ProjectionDefinition CreateProjectionDefinition(
        string projectionName,
        ReadModelIdentifier readModel,
        IDictionary<EventType, FromDefinition> from) =>
        new(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            new ProjectionId(projectionName),
            readModel,
            true,
            true,
            new JsonObject(),
            from,
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), true),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>());
}
