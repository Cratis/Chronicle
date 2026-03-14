// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_constant_key;

public class on_count_attribute : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(DepositToDebitAccountPerformed)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(AccountInfoWithConstantKeyOnCount));

    [Fact]
    void should_have_from_definition_for_event()
    {
        var eventType = event_types.GetEventTypeFor(typeof(DepositToDebitAccountPerformed)).ToContract();
        _result.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_use_constant_key_expression()
    {
        var eventType = event_types.GetEventTypeFor(typeof(DepositToDebitAccountPerformed)).ToContract();
        var fromDefinition = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDefinition.Key.ShouldEqual($"{WellKnownExpressions.Value}(fixed-key)");
    }
}
