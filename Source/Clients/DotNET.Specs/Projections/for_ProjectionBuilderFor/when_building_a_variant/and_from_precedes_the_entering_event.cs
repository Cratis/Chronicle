// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.given;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building_a_variant;

public class and_from_precedes_the_entering_event : events_with_derivatives
{
    ProjectionBuilderFor<Item> _builder;
    ProjectionDefinition _result;

    void Establish() => _builder = new("items", typeof(Item), new DefaultNamingPolicy(), _eventTypes, new JsonSerializerOptions());

    void Because()
    {
        _builder.VariantOf<Container>(_ => _.Id);
        _builder.From<ItemChanged>(from => from.UsingKey(_ => _.Id).UsingParentKey(_ => _.ParentId).Set(_ => _.Name).To(_ => _.Name));
        _builder.EntersOn<FirstItemChanged>();
        _result = _builder.Build();
    }

    [Fact] void should_register_only_the_entering_event_as_from() => _result.From.Keys.Select(_ => _.ToClient()).ShouldContainOnly(typeof(FirstItemChanged).GetEventType());
    [Fact] void should_convert_to_value_keyed_event_types_without_duplicates() => _result.From.ToDictionary(_ => _.Key.ToClient(), _ => _.Value).Count.ShouldEqual(1);
    [Fact] void should_preserve_the_existing_mapping() => _result.From.Single().Value.Properties[nameof(Item.Name)].ShouldEqual(nameof(ItemChanged.Name));
    [Fact] void should_preserve_the_existing_key() => _result.From.Single().Value.Key.ShouldEqual(nameof(ItemChanged.Id));
    [Fact] void should_preserve_the_existing_parent_key() => _result.From.Single().Value.ParentKey.ShouldEqual(nameof(ItemChanged.ParentId));
    [Fact] void should_reclassify_the_other_events_as_joins() => _result.Join.Keys.Select(_ => _.ToClient()).ShouldContainOnly(typeof(ItemChanged).GetEventType(), typeof(SecondItemChanged).GetEventType());
}
