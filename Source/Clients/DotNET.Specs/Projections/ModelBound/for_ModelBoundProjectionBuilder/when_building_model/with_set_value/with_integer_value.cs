// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_set_value;

public class with_integer_value : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Because() => _result = builder.Build(typeof(AccountStatusView));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();

    [Fact]
    void should_have_value_expression_for_tier_property()
    {
        var eventType = event_types.GetEventTypeFor(typeof(DebitAccountOpened)).ToContract();
        var fromDefinition = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDefinition.Properties[nameof(AccountStatusView.Tier)].ShouldEqual("$value(1)");
    }

    record AccountStatusView(
        [Key]
        Guid Id,

        [SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.Name))]
        string Name,

        [SetValue<DebitAccountOpened>(1)]
        int Tier);
}
