// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_from;

public class and_child_has_id_by_convention : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(ItemAddedById)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(OrderWithChildHavingIdByConvention));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_children_definition() => _result.Children.Count.ShouldEqual(1);

    [Fact] void should_use_id_property_as_identified_by()
    {
        var childrenDef = _result.Children[nameof(OrderWithChildHavingIdByConvention.Items)];
        childrenDef.IdentifiedBy.ShouldEqual(nameof(ItemWithIdByConvention.Id));
    }

    [Fact] void should_apply_naming_policy_to_identified_by()
    {
        var childrenDef = _result.Children[nameof(OrderWithChildHavingIdByConvention.Items)];
        childrenDef.IdentifiedBy.ShouldEqual(naming_policy.GetPropertyName(new Properties.PropertyPath(nameof(ItemWithIdByConvention.Id))));
    }
}

[EventType]
public record ItemAddedById(Guid ItemId, string Name);

public record ItemWithIdByConvention(Guid Id, string Name);

public record OrderWithChildHavingIdByConvention(
    [Key] Guid Id,
    [ChildrenFrom<ItemAddedById>(key: nameof(ItemAddedById.ItemId))] IEnumerable<ItemWithIdByConvention> Items);
