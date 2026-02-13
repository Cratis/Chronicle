// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_DeclarativeCodeGenerator.when_generating;

public class with_add_and_subtract_operations : given.a_declarative_code_generator
{
    void Establish()
    {
        var properties = new Dictionary<string, JsonSchemaProperty>
        {
            ["Id"] = new JsonSchemaProperty { Type = JsonObjectType.String },
            ["Balance"] = new JsonSchemaProperty { Type = JsonObjectType.Number }
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

        _definition = CreateProjectionDefinition("AccountProjection", _readModelDefinition.Identifier, from: from);
    }

    void Because() => _result = _generator.Generate(_definition, _readModelDefinition).ToFullString();

    [Fact] void should_generate_code() => _result.ShouldNotBeNull();

    [Fact] void should_contain_add_operation() => _result.ShouldContain(".Add(m => m.Balance).With(e => e.Amount)");

    [Fact] void should_contain_subtract_operation() => _result.ShouldContain(".Subtract(m => m.Balance).With(e => e.Amount)");

    [Fact] void should_contain_from_deposit_made() => _result.ShouldContain(".From<DepositMade>");

    [Fact] void should_contain_from_withdrawal_made() => _result.ShouldContain(".From<WithdrawalMade>");
}
