// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_DeclarativeCodeGenerator.when_generating;

public class with_simple_from_block : given.a_declarative_code_generator
{
    void Establish()
    {
        var properties = new Dictionary<string, JsonSchemaProperty>
        {
            ["Id"] = new JsonSchemaProperty { Type = JsonObjectType.String },
            ["Name"] = new JsonSchemaProperty { Type = JsonObjectType.String }
        };

        _readModelDefinition = CreateReadModelDefinition("Person", properties);

        var personCreatedEvent = CreateEventType("PersonCreated");

        var from = new Dictionary<EventType, FromDefinition>
        {
            [personCreatedEvent] = new FromDefinition(
                new Dictionary<PropertyPath, string>
                {
                    [new PropertyPath("Name")] = "Name"
                },
                new PropertyExpression(WellKnownExpressions.EventSourceId),
                null)
        };

        _definition = CreateProjectionDefinition("PersonProjection", _readModelDefinition.Identifier, from: from);
    }

    void Because() => _result = _generator.Generate(_definition, _readModelDefinition);

    [Fact] void should_generate_code() => _result.ShouldNotBeNull();

    [Fact] void should_contain_using_statement() => _result.ShouldContain("using Cratis.Chronicle.Projections;");

    [Fact] void should_contain_class_declaration() => _result.ShouldContain("public class PersonProjection : IProjectionFor<Person>");

    [Fact] void should_contain_define_method() => _result.ShouldContain("public void Define(IProjectionBuilderFor<Person> builder) => builder");

    [Fact] void should_contain_from_block() => _result.ShouldContain(".From<PersonCreated>");

    [Fact] void should_contain_using_key() => _result.ShouldContain(".UsingKey(e => e.EventSourceId)");

    [Fact] void should_contain_set_operation() => _result.ShouldContain(".Set(m => m.Name)");
}
