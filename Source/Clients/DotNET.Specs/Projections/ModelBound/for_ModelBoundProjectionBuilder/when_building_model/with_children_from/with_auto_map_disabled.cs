// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_from;

public class with_auto_map_disabled : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([
            typeof(DebitAccountOpened),
            typeof(DepositToDebitAccountPerformed),
            typeof(WithdrawalFromDebitAccountPerformed),
            typeof(ItemAddedToCart),
            typeof(ProductItemAdded)
        ]);

        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(OrderWithExplicitMappedChildren));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_children_definition() => _result.Children.Count.ShouldEqual(1);

    [Fact]
    void should_not_auto_map_properties()
    {
        var eventType = event_types.GetEventTypeFor(typeof(ProductItemAdded)).ToContract();
        var childrenDef = _result.Children[nameof(OrderWithExplicitMappedChildren.Items)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;

        // With autoMap disabled, properties should not be automatically mapped
        // Only the Id should be empty since we don't have explicit SetFrom attributes
        fromDef.Properties.Count.ShouldEqual(0);
    }
}

[EventType]
public record ProductItemAdded(Guid ItemId, string ProductName, int Quantity, double Price);

public record ProductLineItem([Key] Guid Id, string ProductName, int Quantity, double Price);

public record OrderWithExplicitMappedChildren(
    [Key] Guid Id,
    [ChildrenFrom<ProductItemAdded>(key: nameof(ProductItemAdded.ItemId), autoMap: AutoMap.Disabled)]
    IEnumerable<ProductLineItem> Items);
