// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_DeclarativeCodeGenerator.when_generating;

public class with_join_block : given.a_declarative_code_generator
{
    void Establish()
    {
        var properties = new Dictionary<string, JsonSchemaProperty>
        {
            ["Id"] = new JsonSchemaProperty { Type = JsonObjectType.String },
            ["CustomerId"] = new JsonSchemaProperty { Type = JsonObjectType.String },
            ["CustomerName"] = new JsonSchemaProperty { Type = JsonObjectType.String }
        };

        _readModelDefinition = CreateReadModelDefinition("OrderSummary", properties);

        var orderPlacedEvent = CreateEventType("OrderPlaced");
        var customerCreatedEvent = CreateEventType("CustomerCreated");

        var from = new Dictionary<EventType, FromDefinition>
        {
            [orderPlacedEvent] = new FromDefinition(
                new Dictionary<PropertyPath, string>
                {
                    [new PropertyPath("CustomerId")] = "CustomerId"
                },
                new PropertyExpression(WellKnownExpressions.EventSourceId),
                null)
        };

        var join = new Dictionary<EventType, JoinDefinition>
        {
            [customerCreatedEvent] = new JoinDefinition(
                new PropertyPath("CustomerId"),
                new Dictionary<PropertyPath, string>
                {
                    [new PropertyPath("CustomerName")] = "Name"
                },
                new PropertyExpression(WellKnownExpressions.EventSourceId))
        };

        _definition = CreateProjectionDefinition("OrderSummaryProjection", _readModelDefinition.Identifier, from: from, join: join);
    }

    void Because() => _result = _generator.Generate(_definition, _readModelDefinition);

    [Fact] void should_generate_code() => _result.ShouldNotBeNull();

    [Fact] void should_contain_join_block() => _result.ShouldContain(".Join<CustomerCreated>");

    [Fact] void should_contain_on_clause() => _result.ShouldContain(".On(m => m.CustomerId)");

    [Fact] void should_contain_set_in_join() => _result.ShouldContain(".Set(m => m.CustomerName).To(e => e.Name)");
}
