// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_set_value;

public class with_string_value : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Because() => _result = builder.Build(typeof(OrderStatusView));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();

    [Fact]
    void should_have_from_definition_for_event()
    {
        var eventType = event_types.GetEventTypeFor(typeof(DebitAccountOpened)).ToContract();
        _result.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_have_value_expression_for_status_property()
    {
        var eventType = event_types.GetEventTypeFor(typeof(DebitAccountOpened)).ToContract();
        var fromDefinition = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDefinition.Properties[nameof(OrderStatusView.Status)].ShouldEqual("$value(active)");
    }

    record OrderStatusView(
        [Key]
        Guid Id,

        [SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.Name))]
        string Name,

        [SetValue<DebitAccountOpened>("active")]
        string Status);
}
