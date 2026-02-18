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

public class with_add_and_subtract_operations : given.a_model_bound_code_generator
{
    void Establish()
    {
        var properties = new Dictionary<string, JsonSchemaProperty>
        {
            ["Id"] = new JsonSchemaProperty { Type = JsonObjectType.String },
            ["Balance"] = new JsonSchemaProperty { Type = JsonObjectType.Integer }
        };

        _readModelDefinition = CreateReadModelDefinition("Account", properties);

        var depositEvent = CreateEventType("DepositMade");
        var withdrawalEvent = CreateEventType("WithdrawalMade");

        var from = new Dictionary<EventType, FromDefinition>
        {
            [depositEvent] = new FromDefinition(
                new Dictionary<PropertyPath, string>
                {
                    [new PropertyPath("Balance")] = $"{WellKnownExpressions.Add}(Amount)"
                },
                new PropertyExpression(WellKnownExpressions.EventSourceId),
                null),
            [withdrawalEvent] = new FromDefinition(
                new Dictionary<PropertyPath, string>
                {
                    [new PropertyPath("Balance")] = $"{WellKnownExpressions.Subtract}(Amount)"
                },
                new PropertyExpression(WellKnownExpressions.EventSourceId),
                null)
        };

        _definition = CreateProjectionDefinition(from: from);
    }

    void Because() => _result = _generator.Generate(_definition, _readModelDefinition);

    [Fact] void should_generate_compilation_unit() => _result.ShouldNotBeNull();

    [Fact] void should_have_add_from_attribute()
    {
        var record = _result.Members.OfType<RecordDeclarationSyntax>().First();
        var balanceParam = record.ParameterList!.Parameters.First(p => p.Identifier.Text == "Balance");
        var hasAddFrom = balanceParam.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString().Contains("AddFrom"));
        hasAddFrom.ShouldBeTrue();
    }

    [Fact] void should_have_subtract_from_attribute()
    {
        var record = _result.Members.OfType<RecordDeclarationSyntax>().First();
        var balanceParam = record.ParameterList!.Parameters.First(p => p.Identifier.Text == "Balance");
        var hasSubtractFrom = balanceParam.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString().Contains("SubtractFrom"));
        hasSubtractFrom.ShouldBeTrue();
    }

    [Fact] void should_have_deposit_made_event_in_add_from()
    {
        var record = _result.Members.OfType<RecordDeclarationSyntax>().First();
        var balanceParam = record.ParameterList!.Parameters.First(p => p.Identifier.Text == "Balance");
        var addFromAttr = balanceParam.AttributeLists
            .SelectMany(al => al.Attributes)
            .First(a => a.Name.ToString().Contains("AddFrom"));
        addFromAttr.Name.ToString().ShouldContain("DepositMade");
    }

    [Fact] void should_have_withdrawal_made_event_in_subtract_from()
    {
        var record = _result.Members.OfType<RecordDeclarationSyntax>().First();
        var balanceParam = record.ParameterList!.Parameters.First(p => p.Identifier.Text == "Balance");
        var subtractFromAttr = balanceParam.AttributeLists
            .SelectMany(al => al.Attributes)
            .First(a => a.Name.ToString().Contains("SubtractFrom"));
        subtractFromAttr.Name.ToString().ShouldContain("WithdrawalMade");
    }
}
