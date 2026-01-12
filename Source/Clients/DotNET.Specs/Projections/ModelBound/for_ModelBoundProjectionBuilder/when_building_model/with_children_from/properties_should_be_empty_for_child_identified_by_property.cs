// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_from;

public class properties_should_be_empty_for_child_identified_by_property : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(SkuItemAddedToOrder)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(OrderWithSkuIdentifiedChild));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();

    [Fact]
    void should_have_children_definition()
    {
        _result.Children.Count.ShouldEqual(1);
    }

    [Fact]
    void should_have_properties_empty_for_identified_by_property()
    {
        var childrenDef = _result.Children[nameof(OrderWithSkuIdentifiedChild.Lines)];
        var eventType = event_types.GetEventTypeFor(typeof(SkuItemAddedToOrder)).ToContract();
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;

        // The properties should be empty since Sku is the identifiedBy property and should not be mapped automatically
        fromDef.Properties.ShouldBeEmpty();
    }

    [Fact]
    void should_use_sku_as_identified_by()
    {
        var childrenDef = _result.Children[nameof(OrderWithSkuIdentifiedChild.Lines)];
        childrenDef.IdentifiedBy.ShouldEqual(nameof(SkuOrderLineItem.Sku));
    }
}

[EventType]
public record SkuItemAddedToOrder(Guid OrderId, string Sku, int Quantity);

public record SkuOrderLineItem(string Sku, int Quantity);

[FromEvent<SkuItemAddedToOrder>]
public record OrderWithSkuIdentifiedChild(
    [Key] Guid Id,
    [ChildrenFrom<SkuItemAddedToOrder>(identifiedBy: nameof(SkuOrderLineItem.Sku), key: nameof(SkuItemAddedToOrder.Sku))] IEnumerable<SkuOrderLineItem> Lines);
