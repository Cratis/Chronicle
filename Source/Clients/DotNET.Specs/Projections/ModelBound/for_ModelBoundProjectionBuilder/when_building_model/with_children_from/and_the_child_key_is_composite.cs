// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_from;

public class and_the_child_key_is_composite : Specification
{
    readonly INamingPolicy _namingPolicy = new CamelCaseNamingPolicy();
    readonly IEventTypes _eventTypes = new EventTypesForSpecifications([typeof(ItemRecorded), typeof(ItemDiscarded), typeof(SingleItemRecorded)]);
    ChildrenDefinition _children;
    FromDefinition _from;
    FromDefinition _fluentFrom;
    FromDefinition _singleKeyFrom;

    void Because()
    {
        var builder = new ModelBoundProjectionBuilder(_namingPolicy, _eventTypes);
        _children = builder.Build(typeof(Catalog)).Children["items"];
        _singleKeyFrom = builder.Build(typeof(SingleKeyCatalog)).Children["items"].From.Single().Value;
        _from = _children.From.Single(pair => pair.Key.IsEqual(_eventTypes.GetEventTypeFor(typeof(ItemRecorded)).ToContract())).Value;
        _fluentFrom = BuildFluentFrom();
    }

    [Fact] void should_match_the_fluent_key() => _from.Key.ShouldEqual(_fluentFrom.Key);
    [Fact] void should_match_the_fluent_parent_key() => _from.ParentKey.ShouldEqual(_fluentFrom.ParentKey);
    [Fact] void should_not_add_mappings_that_overwrite_the_fluent_identifier() => _from.Properties.ShouldEqual(_fluentFrom.Properties);
    [Fact] void should_serialize_each_component() => _from.Key.ShouldEqual("$composite(ownerName=ownerName,itemName=itemName)");
    [Fact] void should_exclude_both_key_parts_from_parent_inference() => _from.ParentKey.ShouldEqual("catalogName");
    [Fact] void should_identify_children_by_the_composite_property() => _children.IdentifiedBy.ShouldEqual("itemKey");
    [Fact] void should_use_the_same_composite_key_to_remove_a_child() => _children.RemovedWith.Single().Value.Key.ShouldEqual(_from.Key);
    [Fact] void should_keep_the_single_key_expression() => _singleKeyFrom.Key.ShouldEqual("itemName");

    FromDefinition BuildFluentFrom()
    {
        var fluent = new ProjectionBuilderFor<Catalog>("catalog", typeof(Catalog), _namingPolicy, _eventTypes, new());
        fluent.Children(model => model.Items, children => children
            .IdentifiedBy(item => item.ItemKey)
            .From<ItemRecorded>(from => from
                .UsingCompositeKey<ItemIdentity>(key => key
                    .Set(part => part.OwnerName).To(@event => @event.OwnerName)
                    .Set(part => part.ItemName).To(@event => @event.ItemName))
                .UsingParentKey(@event => @event.CatalogName)));
        return fluent.Build().Children["items"].From.Single().Value;
    }

    [EventType]
    public record ItemRecorded(string OwnerName, string ItemName, string CatalogName, string Title);

    [EventType]
    public record ItemDiscarded(string OwnerName, string ItemName, string CatalogName);

    [EventType]
    public record SingleItemRecorded(string ItemName, string Title);

    public record ItemIdentity(string OwnerName, string ItemName);

    public record CatalogItem([Key] ItemIdentity ItemKey, string Title);

    public record SingleKeyItem(string ItemName, string Title);

    public record Catalog(
        string Id,
        [ChildrenFrom<ItemRecorded>(CompositeKey = new[] { nameof(ItemRecorded.OwnerName), nameof(ItemRecorded.ItemName) })]
        [RemovedWith<ItemDiscarded>(parentKey: nameof(ItemDiscarded.CatalogName), CompositeKey = new[] { nameof(ItemDiscarded.OwnerName), nameof(ItemDiscarded.ItemName) })]
        IEnumerable<CatalogItem> Items);

    public record SingleKeyCatalog(
        string Id,
        [ChildrenFrom<SingleItemRecorded>(key: nameof(SingleItemRecorded.ItemName))] IEnumerable<SingleKeyItem> Items);
}
