// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.given;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_the_base_event_has_a_later_registration : events_with_derivatives
{
    ProjectionBuilderFor<Item> _builder;
    ProjectionDefinition _result;

    void Establish() => _builder = new("items", typeof(Item), new DefaultNamingPolicy(), _eventTypes, new JsonSerializerOptions());

    void Because()
    {
        _builder.From<FirstItemChanged>(from => from
            .UsingKey(_ => _.Name)
            .UsingParentKey(_ => _.Description)
            .Set(_ => _.Name).To(_ => _.Description)
            .Set(_ => _.LastEventSourceId).ToEventSourceId());
        _builder.From<ItemChanged>(from => from
            .UsingKey(_ => _.Id)
            .UsingParentKey(_ => _.ParentId)
            .Set(_ => _.Id).To(_ => _.Id)
            .Set(_ => _.Name).To(_ => _.Name));
        _result = _builder.Build();
    }

    [Fact] void should_register_each_event_type_once() => _result.From.Keys.Select(_ => _.ToClient()).ShouldContainOnly(typeof(ItemChanged).GetEventType(), typeof(FirstItemChanged).GetEventType(), typeof(SecondItemChanged).GetEventType());
    [Fact] void should_prefer_the_explicit_mapping_for_conflicting_properties() => _result.From.Single(_ => _.Key.ToClient() == typeof(FirstItemChanged).GetEventType()).Value.Properties[nameof(Item.Name)].ShouldEqual(nameof(FirstItemChanged.Description));
    [Fact] void should_preserve_the_disjoint_inherited_mapping() => _result.From.Single(_ => _.Key.ToClient() == typeof(FirstItemChanged).GetEventType()).Value.Properties[nameof(Item.Id)].ShouldEqual(nameof(ItemChanged.Id));
    [Fact] void should_preserve_the_disjoint_explicit_mapping() => _result.From.Single(_ => _.Key.ToClient() == typeof(FirstItemChanged).GetEventType()).Value.Properties[nameof(Item.LastEventSourceId)].ShouldEqual(WellKnownExpressions.EventSourceId);
    [Fact] void should_preserve_the_explicit_key() => _result.From.Single(_ => _.Key.ToClient() == typeof(FirstItemChanged).GetEventType()).Value.Key.ShouldEqual(nameof(FirstItemChanged.Name));
    [Fact] void should_preserve_the_explicit_parent_key() => _result.From.Single(_ => _.Key.ToClient() == typeof(FirstItemChanged).GetEventType()).Value.ParentKey.ShouldEqual(nameof(FirstItemChanged.Description));
    [Fact] void should_preserve_the_mapping_for_the_base_and_other_derivative() => _result.From.Where(_ => _.Key.ToClient() != typeof(FirstItemChanged).GetEventType()).All(_ => _.Value.Properties[nameof(Item.Name)] == nameof(ItemChanged.Name)).ShouldBeTrue();
    [Fact] void should_not_leak_explicit_mappings_to_the_base_and_other_derivative() => _result.From.Where(_ => _.Key.ToClient() != typeof(FirstItemChanged).GetEventType()).All(_ => !_.Value.Properties.ContainsKey(nameof(Item.LastEventSourceId))).ShouldBeTrue();
}
