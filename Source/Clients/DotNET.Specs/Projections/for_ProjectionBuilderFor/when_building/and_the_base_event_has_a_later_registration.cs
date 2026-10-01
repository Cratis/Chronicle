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
        _builder.From<FirstItemChanged>(from => from.Set(_ => _.Name).To(_ => _.Description));
        _builder.From<ItemChanged>(from => from.Set(_ => _.Name).To(_ => _.Name));
        _result = _builder.Build();
    }

    [Fact] void should_register_each_event_type_once() => _result.From.Keys.Select(_ => _.ToClient()).ShouldContainOnly(typeof(ItemChanged).GetEventType(), typeof(FirstItemChanged).GetEventType(), typeof(SecondItemChanged).GetEventType());
    [Fact] void should_use_the_later_mapping_for_every_event() => _result.From.Values.All(_ => _.Properties[nameof(Item.Name)] == nameof(ItemChanged.Name)).ShouldBeTrue();
}
