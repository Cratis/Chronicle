// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.for_ModelBoundCodeGenerator.when_generating;

public class with_single_from_event : given.a_model_bound_code_generator
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

        _definition = CreateProjectionDefinition(from: from);
    }

    void Because() => _result = _generator.Generate(_definition, _readModelDefinition);

    [Fact] void should_generate_compilation_unit() => _result.ShouldNotBeNull();

    [Fact] void should_have_using_directives() => _result.Usings.Count.ShouldEqual(2);

    [Fact] void should_have_keys_using() => _result.Usings.Any(u => u.Name!.ToString() == "Cratis.Chronicle.Keys").ShouldBeTrue();

    [Fact] void should_have_model_bound_using() => _result.Usings.Any(u => u.Name!.ToString() == "Cratis.Chronicle.Projections.ModelBound").ShouldBeTrue();

    [Fact] void should_generate_record() => _result.Members.OfType<RecordDeclarationSyntax>().ShouldNotBeEmpty();

    [Fact] void should_name_record_correctly() => _result.Members.OfType<RecordDeclarationSyntax>().First().Identifier.Text.ShouldEqual("Person");

    [Fact] void should_have_from_event_attribute()
    {
        var record = _result.Members.OfType<RecordDeclarationSyntax>().First();
        var hasFromEvent = record.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString().Contains("FromEvent"));
        hasFromEvent.ShouldBeTrue();
    }

    [Fact] void should_have_id_parameter()
    {
        var record = _result.Members.OfType<RecordDeclarationSyntax>().First();
        record.ParameterList!.Parameters.Any(p => p.Identifier.Text == "Id").ShouldBeTrue();
    }

    [Fact] void should_have_name_parameter()
    {
        var record = _result.Members.OfType<RecordDeclarationSyntax>().First();
        record.ParameterList!.Parameters.Any(p => p.Identifier.Text == "Name").ShouldBeTrue();
    }

    [Fact] void should_have_key_attribute_on_id()
    {
        var record = _result.Members.OfType<RecordDeclarationSyntax>().First();
        var idParam = record.ParameterList!.Parameters.First(p => p.Identifier.Text == "Id");
        var hasKeyAttr = idParam.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString() == "Key");
        hasKeyAttr.ShouldBeTrue();
    }
}
