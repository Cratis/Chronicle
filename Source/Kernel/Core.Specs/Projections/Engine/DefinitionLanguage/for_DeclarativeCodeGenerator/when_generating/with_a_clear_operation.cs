// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_DeclarativeCodeGenerator.when_generating;

/// <summary>
/// A clear renders as the formal Clear rather than as a Set continued by a null constant, so the generated code
/// says what the definition means.
/// </summary>
public class with_a_clear_operation : given.a_declarative_code_generator
{
    void Establish()
    {
        var properties = new Dictionary<string, JsonSchemaProperty>
        {
            ["Id"] = new JsonSchemaProperty { Type = JsonObjectType.String },
            ["Note"] = new JsonSchemaProperty { Type = JsonObjectType.String },
            ["Status"] = new JsonSchemaProperty { Type = JsonObjectType.String }
        };

        _readModelDefinition = CreateReadModelDefinition("NoteView", properties);

        var noteClearedEvent = CreateEventType("NoteCleared");

        var from = new Dictionary<EventType, FromDefinition>
        {
            [noteClearedEvent] = new FromDefinition(
                new Dictionary<PropertyPath, string>
                {
                    [new PropertyPath("Note")] = WellKnownExpressions.Null,
                    [new PropertyPath("Status")] = $"{WellKnownExpressions.Value}(archived)"
                },
                new PropertyExpression(WellKnownExpressions.EventSourceId),
                null)
        };

        _definition = CreateProjectionDefinition("NoteProjection", _readModelDefinition.Identifier, from: from);
    }

    void Because() => _result = _generator.Generate(_definition, _readModelDefinition).ToFullString();

    [Fact] void should_generate_code() => _result.ShouldNotBeNull();
    [Fact] void should_use_clear_for_the_cleared_property() => _result.ShouldContain(".Clear(m => m.Note)");
    [Fact] void should_not_set_the_cleared_property() => _result.ShouldNotContain(".Set(m => m.Note)");
    [Fact] void should_still_use_to_value_for_a_real_constant() => _result.ShouldContain(".Set(m => m.Status).ToValue(\"archived\")");
}
