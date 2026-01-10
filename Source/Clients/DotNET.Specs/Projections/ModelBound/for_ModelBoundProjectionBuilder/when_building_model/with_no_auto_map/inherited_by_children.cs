// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_no_auto_map;

public class inherited_by_children : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([
            typeof(OrderPlaced),
            typeof(OrderItemAdded)
        ]);

        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(OrderWithNoAutoMapChildren));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_children_definition() => _result.Children.Count.ShouldEqual(1);

    [Fact]
    void should_not_auto_map_child_properties()
    {
        var eventType = event_types.GetEventTypeFor(typeof(OrderItemAdded)).ToContract();
        var childrenDef = _result.Children[nameof(OrderWithNoAutoMapChildren.Items)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;

        // Child inherits NoAutoMap from parent through the attribute's Inherited = true
        fromDef.Properties.Count.ShouldEqual(0);
    }
}

[EventType]
public record OrderPlaced(string CustomerName);

[EventType]
public record OrderItemAdded(Guid ItemId, string ProductName, int Quantity);

// Child model inherits NoAutoMap because the attribute is marked with Inherited = true
public record OrderItemWithInheritedNoAutoMap([Key] Guid Id, string ProductName, int Quantity);

[NoAutoMap]
[FromEvent<OrderPlaced>]
public record OrderWithNoAutoMapChildren(
    [Key] Guid Id,
    string CustomerName,
    [ChildrenFrom<OrderItemAdded>(key: nameof(OrderItemAdded.ItemId))]
    IEnumerable<OrderItemWithInheritedNoAutoMap> Items);
