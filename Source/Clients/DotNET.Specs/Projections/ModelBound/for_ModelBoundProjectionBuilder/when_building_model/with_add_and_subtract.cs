// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model;

public class with_add_and_subtract : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Because() => _result = builder.Build(typeof(AccountInfo));

    [Fact]
    void should_have_from_definition_for_deposit()
    {
        var eventType = event_types.GetEventTypeFor(typeof(DepositToDebitAccountPerformed)).ToContract();
        _result.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_use_add_expression_for_balance()
    {
        var eventType = event_types.GetEventTypeFor(typeof(DepositToDebitAccountPerformed)).ToContract();
        var expression = _result.From.Single(kvp => kvp.Key.Id == eventType.Id).Value.Properties[nameof(AccountInfo.Balance)];
        expression.ShouldContain(WellKnownExpressions.Add);
    }

    [Fact]
    void should_have_from_definition_for_withdrawal()
    {
        var eventType = event_types.GetEventTypeFor(typeof(WithdrawalFromDebitAccountPerformed)).ToContract();
        _result.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_use_subtract_expression_for_balance()
    {
        var eventType = event_types.GetEventTypeFor(typeof(WithdrawalFromDebitAccountPerformed)).ToContract();
        var expression = _result.From.Single(kvp => kvp.Key.Id == eventType.Id).Value.Properties[nameof(AccountInfo.Balance)];
        expression.ShouldContain(WellKnownExpressions.Subtract);
    }
}
