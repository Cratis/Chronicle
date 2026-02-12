// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_DeclarativeCodeGenerator.when_generating;

public class with_event_context_property : given.a_declarative_code_generator
{
    void Establish()
    {
        var properties = new Dictionary<string, JsonSchemaProperty>
        {
            ["Id"] = new JsonSchemaProperty { Type = JsonObjectType.String },
            ["LastUpdated"] = new JsonSchemaProperty { Type = JsonObjectType.String }
        };

        _readModelDefinition = CreateReadModelDefinition("Product", properties);

        var productCreatedEvent = CreateEventType("ProductCreated");

        var from = new Dictionary<EventType, FromDefinition>
        {
            [productCreatedEvent] = new FromDefinition(
                new Dictionary<PropertyPath, string>
                {
                    [new PropertyPath("LastUpdated")] = $"{WellKnownExpressions.EventContext}(Occurred)"
                },
                new PropertyExpression(WellKnownExpressions.EventSourceId),
                null)
        };

        _definition = CreateProjectionDefinition("ProductProjection", _readModelDefinition.Identifier, from: from);
    }

    void Because() => _result = _generator.Generate(_definition, _readModelDefinition).ToFullString();

    [Fact] void should_generate_code() => _result.ShouldNotBeNull();

    [Fact] void should_contain_event_context_property() => _result.ShouldContain("ToEventContextProperty(c => c.Occurred)");

    [Fact] void should_contain_set_operation() => _result.ShouldContain(".Set(m => m.LastUpdated)");
}
