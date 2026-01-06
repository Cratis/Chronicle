// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_from;

public class with_auto_map : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([
            typeof(DebitAccountOpened),
            typeof(DepositToDebitAccountPerformed),
            typeof(WithdrawalFromDebitAccountPerformed),
            typeof(ItemAddedToCart),
            typeof(LineItemAdded)
        ]);

        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(OrderWithAutoMappedChildren));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_children_definition() => _result.Children.Count.ShouldEqual(1);
    [Fact] void should_have_children_for_items() => _result.Children.Keys.ShouldContain(nameof(OrderWithAutoMappedChildren.Items));

    [Fact]
    void should_have_from_definition_for_line_item_added()
    {
        var eventType = event_types.GetEventTypeFor(typeof(LineItemAdded)).ToContract();
        var childrenDef = _result.Children[nameof(OrderWithAutoMappedChildren.Items)];
        childrenDef.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_auto_map_product_name()
    {
        var eventType = event_types.GetEventTypeFor(typeof(LineItemAdded)).ToContract();
        var childrenDef = _result.Children[nameof(OrderWithAutoMappedChildren.Items)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties.Keys.ShouldContain(nameof(OrderLineItem.ProductName));
    }

    [Fact]
    void should_auto_map_quantity()
    {
        var eventType = event_types.GetEventTypeFor(typeof(LineItemAdded)).ToContract();
        var childrenDef = _result.Children[nameof(OrderWithAutoMappedChildren.Items)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties.Keys.ShouldContain(nameof(OrderLineItem.Quantity));
    }

    [Fact]
    void should_auto_map_price()
    {
        var eventType = event_types.GetEventTypeFor(typeof(LineItemAdded)).ToContract();
        var childrenDef = _result.Children[nameof(OrderWithAutoMappedChildren.Items)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties.Keys.ShouldContain(nameof(OrderLineItem.Price));
    }

    [Fact]
    void should_apply_naming_policy_to_identified_by()
    {
        var childrenDef = _result.Children[nameof(OrderWithAutoMappedChildren.Items)];
        childrenDef.IdentifiedBy.ShouldEqual(naming_policy.GetPropertyName(new Properties.PropertyPath(nameof(OrderLineItem.Id))));
    }

    [Fact]
    void should_apply_naming_policy_to_key()
    {
        var eventType = event_types.GetEventTypeFor(typeof(LineItemAdded)).ToContract();
        var childrenDef = _result.Children[nameof(OrderWithAutoMappedChildren.Items)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Key.ShouldEqual(naming_policy.GetPropertyName(new Properties.PropertyPath(nameof(LineItemAdded.ItemId))));
    }
}

[EventType]
public record LineItemAdded(Guid ItemId, string ProductName, int Quantity, double Price);

public record OrderLineItem([Key] Guid Id, string ProductName, int Quantity, double Price);

public record OrderWithAutoMappedChildren(
    [Key] Guid Id,
    [ChildrenFrom<LineItemAdded>(key: nameof(LineItemAdded.ItemId))] IEnumerable<OrderLineItem> Items);
