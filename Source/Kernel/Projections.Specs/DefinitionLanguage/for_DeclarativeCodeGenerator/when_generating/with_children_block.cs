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

public class with_children_block : given.a_declarative_code_generator
{
    void Establish()
    {
        var properties = new Dictionary<string, JsonSchemaProperty>
        {
            ["Id"] = new JsonSchemaProperty { Type = JsonObjectType.String },
            ["Name"] = new JsonSchemaProperty { Type = JsonObjectType.String }
        };

        _readModelDefinition = CreateReadModelDefinition("Group", properties);

        var groupCreatedEvent = CreateEventType("GroupCreated");
        var userAddedToGroupEvent = CreateEventType("UserAddedToGroup");

        var from = new Dictionary<EventType, FromDefinition>
        {
            [groupCreatedEvent] = new FromDefinition(
                new Dictionary<PropertyPath, string>
                {
                    [new PropertyPath("Name")] = "Name"
                },
                new PropertyExpression(WellKnownExpressions.EventSourceId),
                null)
        };

        var childrenFrom = new Dictionary<EventType, FromDefinition>
        {
            [userAddedToGroupEvent] = new FromDefinition(
                new Dictionary<PropertyPath, string>
                {
                    [new PropertyPath("Name")] = "UserName"
                },
                new PropertyExpression("UserId"),
                null)
        };

        var children = new Dictionary<PropertyPath, ChildrenDefinition>
        {
            [new PropertyPath("Members")] = new ChildrenDefinition(
                new PropertyPath("UserId"),
                childrenFrom,
                new Dictionary<EventType, JoinDefinition>(),
                new Dictionary<PropertyPath, ChildrenDefinition>(),
                new FromEveryDefinition(new Dictionary<PropertyPath, string>(), true),
                new Dictionary<EventType, RemovedWithDefinition>(),
                new Dictionary<EventType, RemovedWithJoinDefinition>())
        };

        _definition = new ProjectionDefinition(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            new ProjectionId("GroupProjection"),
            _readModelDefinition.Identifier,
            true,
            true,
            new System.Text.Json.Nodes.JsonObject(),
            from,
            new Dictionary<EventType, JoinDefinition>(),
            children,
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), true),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>());
    }

    void Because() => _result = _generator.Generate(_definition, _readModelDefinition);

    [Fact] void should_generate_code() => _result.ShouldNotBeNull();

    [Fact] void should_contain_children_block() => _result.ShouldContain(".Children(m => m.Members");

    [Fact] void should_contain_identified_by() => _result.ShouldContain(".IdentifiedBy(e => e.UserId)");

    [Fact] void should_contain_child_from_block() => _result.ShouldContain(".From<UserAddedToGroup>");

    [Fact] void should_contain_child_set_operation() => _result.ShouldContain(".Set(m => m.Name).To(e => e.UserName)");
}
