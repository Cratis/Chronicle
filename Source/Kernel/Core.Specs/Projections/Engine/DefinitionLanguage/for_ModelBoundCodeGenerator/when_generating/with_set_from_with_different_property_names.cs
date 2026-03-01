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

public class with_set_from_with_different_property_names : given.a_model_bound_code_generator
{
    void Establish()
    {
        var properties = new Dictionary<string, JsonSchemaProperty>
        {
            ["Id"] = new JsonSchemaProperty { Type = JsonObjectType.String },
            ["Balance"] = new JsonSchemaProperty { Type = JsonObjectType.Number }
        };

        _readModelDefinition = CreateReadModelDefinition("AccountInfo", properties);

        var accountOpenedEvent = CreateEventType("AccountOpened");

        var from = new Dictionary<EventType, FromDefinition>
        {
            [accountOpenedEvent] = new FromDefinition(
                new Dictionary<PropertyPath, string>
                {
                    [new PropertyPath("Balance")] = "InitialBalance"
                },
                new PropertyExpression(WellKnownExpressions.EventSourceId),
                null)
        };

        _definition = CreateProjectionDefinition(from: from);
    }

    void Because() => _result = _generator.Generate(_definition, _readModelDefinition);

    [Fact] void should_generate_compilation_unit() => _result.ShouldNotBeNull();

    [Fact] void should_have_set_from_attribute()
    {
        var record = _result.Members.OfType<RecordDeclarationSyntax>().First();
        var balanceParam = record.ParameterList!.Parameters.First(p => p.Identifier.Text == "Balance");
        var hasSetFrom = balanceParam.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString().Contains("SetFrom"));
        hasSetFrom.ShouldBeTrue();
    }

    [Fact] void should_have_nameof_expression_for_different_property_name()
    {
        var record = _result.Members.OfType<RecordDeclarationSyntax>().First();
        var balanceParam = record.ParameterList!.Parameters.First(p => p.Identifier.Text == "Balance");
        var setFromAttr = balanceParam.AttributeLists
            .SelectMany(al => al.Attributes)
            .First(a => a.Name.ToString().Contains("SetFrom"));

        setFromAttr.ArgumentList.ShouldNotBeNull();
        var code = setFromAttr.ArgumentList!.ToString();
        code.ShouldContain("nameof");
        code.ShouldContain("InitialBalance");
    }
}
