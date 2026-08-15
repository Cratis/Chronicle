// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_clear;

/// <summary>
/// The control for the refusal: a nullable value type has the state the clear writes, so the same declaration that
/// is refused on `int` is accepted on `int?`. Without this pair the refusal could just as well be rejecting every
/// value type, or every clear.
/// </summary>
public class with_a_clear_on_a_nullable_value_type : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Because() => _result = builder.Build(typeof(NullableCountView));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();

    [Fact]
    void should_have_a_clear_expression_for_the_count_property()
    {
        var eventType = event_types.GetEventTypeFor(typeof(WithdrawalFromDebitAccountPerformed)).ToContract();
        var fromDefinition = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDefinition.Properties[nameof(NullableCountView.Count)].ShouldEqual(WellKnownExpressions.Null);
    }

    record NullableCountView(
        [Key]
        Guid Id,

        [ClearWith<WithdrawalFromDebitAccountPerformed>]
        int? Count);
}
