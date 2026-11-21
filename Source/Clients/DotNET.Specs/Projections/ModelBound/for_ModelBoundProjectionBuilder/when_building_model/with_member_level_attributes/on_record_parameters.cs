// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_member_level_attributes;

/// <summary>
/// Parameter-level declarations on a positional record: mapped through the parameter path of the builder. Must produce the same definition as the property-level declarations.
/// </summary>
public class on_record_parameters : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Because() => _result = builder.Build(typeof(Model));

    [Fact]
    void should_map_the_name_from_the_event()
    {
        var eventType = event_types.GetEventTypeFor(typeof(DebitAccountOpened)).ToContract();
        _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value.Properties[nameof(Model.Name)].ShouldEqual(nameof(DebitAccountOpened.Name));
    }

    [Fact]
    void should_map_the_constant_set_value()
    {
        var eventType = event_types.GetEventTypeFor(typeof(DepositToDebitAccountPerformed)).ToContract();
        _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value.Properties[nameof(Model.Status)].ShouldEqual($"{WellKnownExpressions.Value}(Funded)");
    }

    [Fact]
    void should_clear_the_note_with_a_null_set_value()
    {
        var eventType = event_types.GetEventTypeFor(typeof(WithdrawalFromDebitAccountPerformed)).ToContract();
        _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value.Properties[nameof(Model.Note)].ShouldEqual(WellKnownExpressions.Null);
    }

    [Fact]
    void should_use_the_constant_key_for_the_count()
    {
        var eventType = event_types.GetEventTypeFor(typeof(ItemAddedToCart)).ToContract();
        _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value.Key.ShouldEqual($"{WellKnownExpressions.Value}(fixed-key)");
    }

    [Fact]
    void should_map_the_every_event_context_property()
    {
        _result.All.Properties[nameof(Model.LastSeen)].ShouldEqual($"{WellKnownExpressions.EventContext}({nameof(EventContext.Occurred)})");
        _result.All.IncludeChildren.ShouldBeTrue();
    }

    [Fact] void should_not_subscribe_to_all_events() => _result.SubscribesToAllEvents.ShouldBeFalse();
    [Fact] void should_remove_the_whole_document_on_the_root_removed_with() => _result.RemovedWith.Count.ShouldEqual(1);
    [Fact] void should_not_put_the_children_removal_on_the_root() => _result.RemovedWith.Keys.Single().Id.ShouldEqual(event_types.GetEventTypeFor(typeof(ReadModelRemoved)).Id.ToString());
    [Fact] void should_have_the_children_collection() => _result.Children.ContainsKey(nameof(Model.Items)).ShouldBeTrue();
    [Fact] void should_route_the_children_removal_to_the_child_definition() => _result.Children[nameof(Model.Items)].RemovedWith.Keys.Single().Id.ShouldEqual(event_types.GetEventTypeFor(typeof(ChildItemRemoved)).Id.ToString());
    [Fact] void should_route_the_children_join_removal_to_the_child_definition() => _result.Children[nameof(Model.Items)].RemovedWithJoin.Keys.Single().Id.ShouldEqual(event_types.GetEventTypeFor(typeof(ChildItemRemovedJoin)).Id.ToString());
    [Fact] void should_not_have_the_children_join_removal_on_the_root() => _result.RemovedWithJoin.Count.ShouldEqual(0);
    [Fact] void should_have_the_nested_object() => _result.Nested.ContainsKey(nameof(Model.Owner)).ShouldBeTrue();

    record Model(
        [Key]
        Guid Id,

        [SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.Name))]
        string Name,

        [SetValue<DepositToDebitAccountPerformed>("Funded")]
        string Status,

        [SetValue<WithdrawalFromDebitAccountPerformed>(null)]
        [RemovedWith<ReadModelRemoved>]
        string? Note,

        [Count<ItemAddedToCart>(ConstantKey = "fixed-key")]
        int Count,

        [FromEvery(contextProperty: nameof(EventContext.Occurred))]
        DateTimeOffset? LastSeen,

        [ChildrenFrom<ItemAddedToCart>(key: nameof(ItemAddedToCart.ItemId), identifiedBy: nameof(PropertyRemovableChildItem.Id))]
        [RemovedWith<ChildItemRemoved>(key: nameof(ChildItemRemoved.ItemId), parentKey: nameof(ChildItemRemoved.ParentId))]
        [RemovedWithJoin<ChildItemRemovedJoin>]
        IEnumerable<PropertyRemovableChildItem> Items,

        [Nested]
        Owner? Owner);

    record Owner(string Name);
}
