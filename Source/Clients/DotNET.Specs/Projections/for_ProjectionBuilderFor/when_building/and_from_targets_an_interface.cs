// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.given;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_from_targets_an_interface : events_with_derivatives
{
    ProjectionBuilderFor<Item> _builder;
    ProjectionDefinition _result;

    void Establish() => _builder = new("items", typeof(Item), new DefaultNamingPolicy(), _eventTypes, new JsonSerializerOptions());

    void Because()
    {
        _builder.From<IItemChanged>();
        _result = _builder.Build();
    }

    [Fact] void should_register_only_the_concrete_event_types() => _result.From.Keys.Select(_ => _.ToClient()).ShouldContainOnly(typeof(ItemChanged).GetEventType(), typeof(FirstItemChanged).GetEventType(), typeof(SecondItemChanged).GetEventType());
    [Fact] void should_use_the_default_key_for_every_event() => _result.From.Values.All(_ => _.Key == WellKnownExpressions.EventSourceId).ShouldBeTrue();
    [Fact] void should_leave_property_mapping_to_automap() => _result.From.Values.All(_ => _.Properties.Count == 0).ShouldBeTrue();
    [Fact] void should_keep_automap_enabled() => _result.AutoMap.ShouldEqual(Contracts.Projections.AutoMap.Enabled);
}
