// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.given;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_NestedBuilder.when_building;

public class and_from_targets_an_event_with_derivatives : events_with_derivatives
{
    NestedBuilder<Container, Item> _builder;
    ChildrenDefinition _result;

    void Establish() => _builder = new(new DefaultNamingPolicy(), _eventTypes, new JsonSerializerOptions(), AutoMap.Inherit);

    void Because()
    {
        _builder.From<ItemChanged>(from => from.UsingKey(_ => _.Id).UsingParentKey(_ => _.ParentId).Set(_ => _.Name).To(_ => _.Name));
        _builder.FromEvery(_ => _.Set(model => model.LastEventSourceId).ToEventSourceId());
        _result = _builder.Build();
    }

    [Fact] void should_register_the_base_and_both_derivatives() => _result.From.Keys.Select(_ => _.ToClient()).ShouldContainOnly(typeof(ItemChanged).GetEventType(), typeof(FirstItemChanged).GetEventType(), typeof(SecondItemChanged).GetEventType());
    [Fact] void should_preserve_the_mapping_for_each_event() => _result.From.Values.All(_ => _.Properties[nameof(Item.Name)] == nameof(ItemChanged.Name)).ShouldBeTrue();
    [Fact] void should_preserve_the_key_for_each_event() => _result.From.Values.All(_ => _.Key == nameof(ItemChanged.Id)).ShouldBeTrue();
    [Fact] void should_preserve_the_parent_key_for_each_event() => _result.From.Values.All(_ => _.ParentKey == nameof(ItemChanged.ParentId)).ShouldBeTrue();
    [Fact] void should_not_add_a_child_identity() => _result.IdentifiedBy.ShouldEqual((string)PropertyPath.NotSet);
    [Fact] void should_keep_from_every_separate() => _result.All.Properties[nameof(Item.LastEventSourceId)].ShouldEqual(WellKnownExpressions.EventSourceId);
}
