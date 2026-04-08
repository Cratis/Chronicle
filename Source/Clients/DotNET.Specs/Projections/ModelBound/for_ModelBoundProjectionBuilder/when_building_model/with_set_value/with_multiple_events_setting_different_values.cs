// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_set_value;

public class with_multiple_events_setting_different_values : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([
            typeof(DebitAccountOpened),
            typeof(DepositToDebitAccountPerformed)
        ]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(AccountStateView));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();

    [Fact]
    void should_have_from_definitions_for_both_events()
    {
        _result.From.Count.ShouldEqual(2);
    }

    [Fact]
    void should_set_active_when_account_opened()
    {
        var eventType = event_types.GetEventTypeFor(typeof(DebitAccountOpened)).ToContract();
        var fromDefinition = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDefinition.Properties[nameof(AccountStateView.IsActive)].ShouldEqual("$value(True)");
    }

    [Fact]
    void should_set_false_when_deposit_performed()
    {
        var eventType = event_types.GetEventTypeFor(typeof(DepositToDebitAccountPerformed)).ToContract();
        var fromDefinition = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDefinition.Properties[nameof(AccountStateView.IsActive)].ShouldEqual("$value(False)");
    }

    record AccountStateView(
        [Key]
        Guid Id,

        [SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.Name))]
        string Name,

        [SetValue<DebitAccountOpened>(true)]
        [SetValue<DepositToDebitAccountPerformed>(false)]
        bool IsActive);
}
