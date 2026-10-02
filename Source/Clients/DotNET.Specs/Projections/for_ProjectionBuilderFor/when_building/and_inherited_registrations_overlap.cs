// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.given;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_inherited_registrations_overlap : events_with_derivatives
{
    ProjectionBuilderFor<Item> _builder;
    ProjectionDefinition _result;

    void Establish() => _builder = new("items", typeof(Item), new DefaultNamingPolicy(), _eventTypes, new JsonSerializerOptions());

    void Because()
    {
        _builder.From<IItemChanged>(from => from
            .UsingKey(_ => _.Name)
            .UsingParentKey(_ => _.Name)
            .Set(_ => _.Id).To(_ => _.Name)
            .Set(_ => _.Name).To(_ => _.Name));
        _builder.From<ItemChanged>(from => from
            .UsingKey(_ => _.Id)
            .UsingParentKey(_ => _.ParentId)
            .Set(_ => _.Name).To(_ => _.ParentId)
            .Set(_ => _.LastEventSourceId).ToEventSourceId());
        _result = _builder.Build();
    }

    [Fact] void should_register_each_event_type_once() => _result.From.Keys.Select(_ => _.ToClient()).ShouldContainOnly(typeof(ItemChanged).GetEventType(), typeof(FirstItemChanged).GetEventType(), typeof(SecondItemChanged).GetEventType());
    [Fact] void should_preserve_the_earlier_disjoint_mapping() => _result.From.Single(_ => _.Key.ToClient() == typeof(FirstItemChanged).GetEventType()).Value.Properties[nameof(Item.Id)].ShouldEqual(nameof(IItemChanged.Name));
    [Fact] void should_preserve_the_later_disjoint_mapping() => _result.From.Single(_ => _.Key.ToClient() == typeof(FirstItemChanged).GetEventType()).Value.Properties[nameof(Item.LastEventSourceId)].ShouldEqual(WellKnownExpressions.EventSourceId);
    [Fact] void should_prefer_the_later_inherited_mapping_for_conflicting_properties() => _result.From.Single(_ => _.Key.ToClient() == typeof(FirstItemChanged).GetEventType()).Value.Properties[nameof(Item.Name)].ShouldEqual(nameof(ItemChanged.ParentId));
    [Fact] void should_preserve_the_first_inherited_key() => _result.From.Single(_ => _.Key.ToClient() == typeof(FirstItemChanged).GetEventType()).Value.Key.ShouldEqual(nameof(IItemChanged.Name));
    [Fact] void should_preserve_the_first_inherited_parent_key() => _result.From.Single(_ => _.Key.ToClient() == typeof(FirstItemChanged).GetEventType()).Value.ParentKey.ShouldEqual(nameof(IItemChanged.Name));
}
