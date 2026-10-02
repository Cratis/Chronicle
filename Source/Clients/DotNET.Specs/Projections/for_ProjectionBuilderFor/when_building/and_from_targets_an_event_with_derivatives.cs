// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.given;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_from_targets_an_event_with_derivatives : events_with_derivatives
{
    ProjectionBuilderFor<Item> _builder;
    ProjectionDefinition _result;
    int _callbackCount;

    void Establish() => _builder = new("items", typeof(Item), new DefaultNamingPolicy(), _eventTypes, new JsonSerializerOptions());

    void Because()
    {
        _builder.From<ItemChanged>(from =>
        {
            _callbackCount++;
            from.UsingKey(_ => _.Id).UsingParentKey(_ => _.ParentId).Set(_ => _.Name).To(_ => _.Name);
        });
        _builder.From<OtherEvent>();
        _builder.FromEvery(_ => _.Set(model => model.LastEventSourceId).ToEventSourceId());
        _result = _builder.Build();
    }

    [Fact] void should_register_the_base_and_both_derivatives_alongside_the_unrelated_event() => _result.From.Keys.Select(_ => _.ToClient()).ShouldContainOnly(_eventTypes.All);
    [Fact] void should_preserve_the_mapping_for_each_derivative() => _result.From.Where(_ => _.Key.Id != typeof(OtherEvent).GetEventType().Id).All(_ => _.Value.Properties[nameof(Item.Name)] == nameof(ItemChanged.Name)).ShouldBeTrue();
    [Fact] void should_preserve_the_key_for_each_derivative() => _result.From.Where(_ => _.Key.Id != typeof(OtherEvent).GetEventType().Id).All(_ => _.Value.Key == nameof(ItemChanged.Id)).ShouldBeTrue();
    [Fact] void should_preserve_the_parent_key_for_each_derivative() => _result.From.Where(_ => _.Key.Id != typeof(OtherEvent).GetEventType().Id).All(_ => _.Value.ParentKey == nameof(ItemChanged.ParentId)).ShouldBeTrue();
    [Fact] void should_invoke_the_mapping_callback_once() => _callbackCount.ShouldEqual(1);
    [Fact] void should_not_register_the_same_derivatives_twice() => _result.FromEvery.ShouldBeEmpty();
    [Fact] void should_keep_from_every_separate() => _result.All.Properties[nameof(Item.LastEventSourceId)].ShouldEqual(WellKnownExpressions.EventSourceId);
    [Fact] void should_not_subscribe_to_unregistered_events() => _result.SubscribesToAllEvents.ShouldBeFalse();
}
