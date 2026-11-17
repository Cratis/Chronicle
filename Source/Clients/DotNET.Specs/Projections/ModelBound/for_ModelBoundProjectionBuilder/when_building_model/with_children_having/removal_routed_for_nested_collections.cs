// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_having;

public class removal_routed_for_nested_collections : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([
            typeof(ShelfCreated),
            typeof(BoxAdded),
            typeof(BoxDiscarded),
            typeof(CrateAdded),
            typeof(ItemAdded),
            typeof(ItemRemoved)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(Shelf));

    [Fact]
    void should_remove_the_box_with_the_class_level_removed_with()
    {
        var boxDiscarded = event_types.GetEventTypeFor(typeof(BoxDiscarded)).ToContract();
        _result.Children[nameof(Shelf.Boxes)].RemovedWith.Keys.ShouldContain(et => et.IsEqual(boxDiscarded));
    }

    [Fact]
    void should_not_remove_the_box_with_the_removal_of_its_item_collection()
    {
        var itemRemoved = event_types.GetEventTypeFor(typeof(ItemRemoved)).ToContract();
        _result.Children[nameof(Shelf.Boxes)].RemovedWith.Keys.ShouldNotContain(et => et.IsEqual(itemRemoved));
    }

    [Fact]
    void should_remove_the_item_from_the_constructor_parameter_collection()
    {
        var itemRemoved = event_types.GetEventTypeFor(typeof(ItemRemoved)).ToContract();
        var items = _result.Children[nameof(Shelf.Boxes)].Children[nameof(Box.Items)];
        items.RemovedWith.Keys.ShouldContain(et => et.IsEqual(itemRemoved));
    }

    [Fact]
    void should_map_the_removal_key_on_the_constructor_parameter_collection()
    {
        var itemRemoved = event_types.GetEventTypeFor(typeof(ItemRemoved)).ToContract();
        var items = _result.Children[nameof(Shelf.Boxes)].Children[nameof(Box.Items)];
        items.RemovedWith.Single(kvp => kvp.Key.IsEqual(itemRemoved)).Value.Key.ShouldEqual(nameof(ItemRemoved.ItemId));
    }

    [Fact]
    void should_not_remove_the_crate_with_the_removal_of_its_item_collection()
    {
        var itemRemoved = event_types.GetEventTypeFor(typeof(ItemRemoved)).ToContract();
        _result.Children[nameof(Shelf.Crates)].RemovedWith.Keys.ShouldNotContain(et => et.IsEqual(itemRemoved));
    }

    [Fact]
    void should_remove_the_item_from_the_property_collection()
    {
        var itemRemoved = event_types.GetEventTypeFor(typeof(ItemRemoved)).ToContract();
        var items = _result.Children[nameof(Shelf.Crates)].Children[nameof(Crate.Items)];
        items.RemovedWith.Keys.ShouldContain(et => et.IsEqual(itemRemoved));
    }
}

[EventType]
public record ShelfCreated(Guid Id);

[EventType]
public record BoxAdded(Guid ShelfId, Guid BoxId);

[EventType]
public record BoxDiscarded(Guid BoxId);

[EventType]
public record CrateAdded(Guid ShelfId, Guid CrateId);

[EventType]
public record ItemAdded(Guid ParentId, Guid ItemId);

[EventType]
public record ItemRemoved(Guid ItemId);

public record Item([Key] Guid Id);

[RemovedWith<BoxDiscarded>(key: nameof(BoxDiscarded.BoxId))]
public record Box(
    [Key] Guid Id,
    [ChildrenFrom<ItemAdded>(key: nameof(ItemAdded.ItemId), parentKey: nameof(ItemAdded.ParentId))]
    [RemovedWith<ItemRemoved>(key: nameof(ItemRemoved.ItemId))]
    IEnumerable<Item> Items);

public class Crate
{
    [Key]
    public Guid Id { get; set; }

    [ChildrenFrom<ItemAdded>(key: nameof(ItemAdded.ItemId), parentKey: nameof(ItemAdded.ParentId))]
    [RemovedWith<ItemRemoved>(key: nameof(ItemRemoved.ItemId))]
    public IEnumerable<Item> Items { get; set; } = [];
}

[FromEvent<ShelfCreated>]
public record Shelf(
    Guid Id,
    [ChildrenFrom<BoxAdded>(key: nameof(BoxAdded.BoxId), parentKey: nameof(BoxAdded.ShelfId))]
    IEnumerable<Box> Boxes,
    [ChildrenFrom<CrateAdded>(key: nameof(CrateAdded.CrateId), parentKey: nameof(CrateAdded.ShelfId))]
    IEnumerable<Crate> Crates);

#pragma warning restore SA1402 // File may only contain a single type
