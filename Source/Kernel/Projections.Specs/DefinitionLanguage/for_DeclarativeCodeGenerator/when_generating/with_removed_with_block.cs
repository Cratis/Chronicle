// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_DeclarativeCodeGenerator.when_generating;

public class with_removed_with_block : given.a_declarative_code_generator
{
    void Establish()
    {
        var properties = new Dictionary<string, JsonSchemaProperty>
        {
            ["Id"] = new JsonSchemaProperty { Type = JsonObjectType.String },
            ["Name"] = new JsonSchemaProperty { Type = JsonObjectType.String }
        };

        _readModelDefinition = CreateReadModelDefinition("Account", properties);

        var accountOpenedEvent = CreateEventType("AccountOpened");
        var accountClosedEvent = CreateEventType("AccountClosed");

        var from = new Dictionary<EventType, FromDefinition>
        {
            [accountOpenedEvent] = new FromDefinition(
                new Dictionary<PropertyPath, string>
                {
                    [new PropertyPath("Name")] = "Name"
                },
                new PropertyExpression(WellKnownExpressions.EventSourceId),
                null)
        };

        var removedWith = new Dictionary<EventType, RemovedWithDefinition>
        {
            [accountClosedEvent] = new RemovedWithDefinition(
                new PropertyExpression(WellKnownExpressions.EventSourceId),
                null)
        };

        _definition = new ProjectionDefinition(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            new ProjectionId("AccountProjection"),
            _readModelDefinition.Identifier,
            true,
            true,
            new System.Text.Json.Nodes.JsonObject(),
            from,
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), true),
            removedWith,
            new Dictionary<EventType, RemovedWithJoinDefinition>());
    }

    void Because() => _result = _generator.Generate(_definition, _readModelDefinition).ToFullString();

    [Fact] void should_generate_code() => _result.ShouldNotBeNull();

    [Fact] void should_contain_removed_with_block() => _result.ShouldContain(".RemovedWith<AccountClosed>");

    [Fact] void should_contain_event_source_id_in_removed_with() => _result.ShouldContain("e => e.EventSourceId");
}
