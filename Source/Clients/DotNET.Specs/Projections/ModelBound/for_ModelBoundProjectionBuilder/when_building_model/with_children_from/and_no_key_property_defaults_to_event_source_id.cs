// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_from;

public class and_no_key_property_defaults_to_event_source_id : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(ItemAddedWithoutKey)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(OrderWithChildHavingNoKey));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_children_definition() => _result.Children.Count.ShouldEqual(1);

    [Fact] void should_default_to_event_source_id()
    {
        var childrenDef = _result.Children[nameof(OrderWithChildHavingNoKey.Items)];
        childrenDef.IdentifiedBy.ShouldEqual(WellKnownExpressions.EventSourceId);
    }
}

[EventType]
public record ItemAddedWithoutKey(Guid ItemId, string Name);

public record ItemWithoutKey(string Name, string Description);

public record OrderWithChildHavingNoKey(
    [Key] Guid Id,
    [ChildrenFrom<ItemAddedWithoutKey>(key: nameof(ItemAddedWithoutKey.ItemId))] IEnumerable<ItemWithoutKey> Items);
