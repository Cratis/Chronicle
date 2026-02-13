// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_DeclarativeCodeGenerator.when_generating;

public class with_composite_key : given.a_declarative_code_generator
{
    void Establish()
    {
        var properties = new Dictionary<string, JsonSchemaProperty>
        {
            ["Id"] = new JsonSchemaProperty { Type = JsonObjectType.String },
            ["Name"] = new JsonSchemaProperty { Type = JsonObjectType.String }
        };

        _readModelDefinition = CreateReadModelDefinition("Order", properties);

        var orderCreatedEvent = CreateEventType("OrderCreated");

        var from = new Dictionary<EventType, FromDefinition>
        {
            [orderCreatedEvent] = new FromDefinition(
                new Dictionary<PropertyPath, string>
                {
                    [new PropertyPath("Name")] = "CustomerName"
                },
                new PropertyExpression($"{WellKnownExpressions.Composite}(OrderKey, CustomerId=CustomerId, OrderNumber=OrderNumber)"),
                null)
        };

        _definition = CreateProjectionDefinition("OrderProjection", _readModelDefinition.Identifier, from: from);
    }

    void Because() => _result = _generator.Generate(_definition, _readModelDefinition).ToFullString();

    [Fact] void should_generate_code() => _result.ShouldNotBeNull();

    [Fact] void should_contain_composite_key() => _result.ShouldContain(".UsingCompositeKey<OrderKey>");

    [Fact] void should_contain_customer_id_mapping() => _result.ShouldContain(".Set(k => k.CustomerId).To(e => e.CustomerId)");

    [Fact] void should_contain_order_number_mapping() => _result.ShouldContain(".Set(k => k.OrderNumber).To(e => e.OrderNumber)");
}
