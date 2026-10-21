// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.for_EventGenerationRelease.when_releasing;

public class and_different_event_types_pin_different_generations : given.a_release_boundary
{
    readonly EventType _other = new("person-renamed", 1);

    void Establish()
    {
        _schemas[_other] = new(_other, EventTypeOwner.Client, EventTypeSource.Code, _firstSchema);
        _eventTypes.GetDefinition(_other.Id).Returns(_definition with { Id = _other.Id });
    }

    async Task Because() => _result = await _release.Release("store", [_other, _pin], _schemas, [_event with { Context = _event.Context with { EventType = _other } }, _event]);

    [Fact] void should_deliver_each_event_once_in_its_pin() => _result.Select(@event => @event.Context.EventType).ShouldContainOnly(_other, _pin);
    [Fact] void should_release_the_first_shape() => ((IDictionary<string, object?>)_result[0].Content)["name"].ShouldEqual("Ada Lovelace");
    [Fact] void should_release_the_second_shape() => ((IDictionary<string, object?>)_result[1].Content)["fullName"].ShouldEqual("Ada Lovelace");
    [Fact] void should_select_each_events_hash() => _result.Select(@event => @event.Context.Hash.Value).ShouldContainOnly("first-hash", "second-hash");
}
