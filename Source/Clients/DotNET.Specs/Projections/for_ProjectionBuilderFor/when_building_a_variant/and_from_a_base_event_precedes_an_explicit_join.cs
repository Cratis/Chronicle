// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.given;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building_a_variant;

public class and_from_a_base_event_precedes_an_explicit_join : events_with_derivatives
{
    ProjectionBuilderFor<Item> _builder;
    ProjectionDefinition _result;

    void Establish() => _builder = new("items", typeof(Item), new DefaultNamingPolicy(), _eventTypes, new JsonSerializerOptions());

    void Because()
    {
        _builder.VariantOf<Container>(_ => _.Id).EntersOn<FirstItemChanged>();
        _builder.From<ItemChanged>(from => from
            .UsingKey(_ => _.Id)
            .Set(_ => _.Id).To(_ => _.Id)
            .Set(_ => _.Name).To(_ => _.Name));
        _builder.Join<SecondItemChanged>(join => join
            .On(_ => _.LastEventSourceId)
            .UsingKey(_ => _.ParentId)
            .Set(_ => _.Name).To(_ => _.ParentId)
            .Set(_ => _.LastEventSourceId).ToEventSourceId());
        _result = _builder.Build();
    }

    [Fact] void should_register_only_the_entering_event_as_from() => _result.From.Keys.Select(_ => _.ToClient()).ShouldContainOnly(typeof(FirstItemChanged).GetEventType());
    [Fact] void should_reclassify_the_other_events_as_joins() => _result.Join.Keys.Select(_ => _.ToClient()).ShouldContainOnly(typeof(ItemChanged).GetEventType(), typeof(SecondItemChanged).GetEventType());
    [Fact] void should_convert_to_value_keyed_event_types_without_duplicates() => _result.Join.ToDictionary(_ => _.Key.ToClient(), _ => _.Value).Count.ShouldEqual(2);
    [Fact] void should_preserve_the_inherited_disjoint_mapping() => _result.Join.Single(_ => _.Key.ToClient() == typeof(SecondItemChanged).GetEventType()).Value.Properties[nameof(Item.Id)].ShouldEqual(nameof(ItemChanged.Id));
    [Fact] void should_preserve_the_explicit_disjoint_mapping() => _result.Join.Single(_ => _.Key.ToClient() == typeof(SecondItemChanged).GetEventType()).Value.Properties[nameof(Item.LastEventSourceId)].ShouldEqual(WellKnownExpressions.EventSourceId);
    [Fact] void should_prefer_the_explicit_join_for_conflicting_properties() => _result.Join.Single(_ => _.Key.ToClient() == typeof(SecondItemChanged).GetEventType()).Value.Properties[nameof(Item.Name)].ShouldEqual(nameof(SecondItemChanged.ParentId));
    [Fact] void should_preserve_the_explicit_join_key() => _result.Join.Single(_ => _.Key.ToClient() == typeof(SecondItemChanged).GetEventType()).Value.Key.ShouldEqual(nameof(SecondItemChanged.ParentId));
    [Fact] void should_preserve_the_explicit_join_on_property() => _result.Join.Single(_ => _.Key.ToClient() == typeof(SecondItemChanged).GetEventType()).Value.On.ShouldEqual(nameof(Item.LastEventSourceId));
    [Fact] void should_preserve_the_mapping_for_the_other_reclassified_event() => _result.Join.Single(_ => _.Key.ToClient() == typeof(ItemChanged).GetEventType()).Value.Properties[nameof(Item.Name)].ShouldEqual(nameof(ItemChanged.Name));
    [Fact] void should_use_the_variant_key_for_the_other_reclassified_event() => _result.Join.Single(_ => _.Key.ToClient() == typeof(ItemChanged).GetEventType()).Value.On.ShouldEqual(nameof(Item.Id));
    [Fact] void should_not_leak_the_explicit_join_mapping_to_other_events() => _result.Join.Single(_ => _.Key.ToClient() == typeof(ItemChanged).GetEventType()).Value.Properties.ContainsKey(nameof(Item.LastEventSourceId)).ShouldBeFalse();
}
