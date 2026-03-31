// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_DeclarativeCodeGenerator.when_generating;

public class with_set_value_operations : given.a_declarative_code_generator
{
    void Establish()
    {
        var properties = new Dictionary<string, JsonSchemaProperty>
        {
            ["Id"] = new JsonSchemaProperty { Type = JsonObjectType.String },
            ["Status"] = new JsonSchemaProperty { Type = JsonObjectType.String },
            ["Priority"] = new JsonSchemaProperty { Type = JsonObjectType.Integer },
            ["IsActive"] = new JsonSchemaProperty { Type = JsonObjectType.Boolean }
        };

        _readModelDefinition = CreateReadModelDefinition("TaskView", properties);

        var taskCreatedEvent = CreateEventType("TaskCreated");

        var from = new Dictionary<EventType, FromDefinition>
        {
            [taskCreatedEvent] = new FromDefinition(
                new Dictionary<PropertyPath, string>
                {
                    [new PropertyPath("Status")] = $"{WellKnownExpressions.Value}(active)",
                    [new PropertyPath("Priority")] = $"{WellKnownExpressions.Value}(1)",
                    [new PropertyPath("IsActive")] = $"{WellKnownExpressions.Value}(True)"
                },
                new PropertyExpression(WellKnownExpressions.EventSourceId),
                null)
        };

        _definition = CreateProjectionDefinition("TaskProjection", _readModelDefinition.Identifier, from: from);
    }

    void Because() => _result = _generator.Generate(_definition, _readModelDefinition).ToFullString();

    [Fact] void should_generate_code() => _result.ShouldNotBeNull();

    [Fact] void should_use_to_value_for_string_constant() => _result.ShouldContain("ToValue(\"active\")");

    [Fact] void should_use_to_value_for_integer_constant() => _result.ShouldContain("ToValue(1)");

    [Fact] void should_use_to_value_for_boolean_constant() => _result.ShouldContain("ToValue(true)");

    [Fact] void should_contain_set_operation_for_status() => _result.ShouldContain(".Set(m => m.Status)");

    [Fact] void should_contain_set_operation_for_priority() => _result.ShouldContain(".Set(m => m.Priority)");

    [Fact] void should_contain_set_operation_for_is_active() => _result.ShouldContain(".Set(m => m.IsActive)");
}
