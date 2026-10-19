// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventsResultHandler.when_handling;

public class and_keys_are_supported : given.a_reactor_delivery_with_an_event_log
{
    object _first;
    object _second;
    Result<ReactorSideEffectFailure> _result;
    List<Cratis.Chronicle.EventSequences.EventForEventSourceId> _sent;

    void Establish()
    {
        _first = new object();
        _second = new object();
    }

    async Task Because()
    {
        _result = await new EventsResultHandler().Handle(_reactorContext, _eventStore, new[] { _first, _second });
        _sent = [.. KeyedBatches()];
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_send_every_event() => _sent.Count.ShouldEqual(2);
    [Fact] void should_send_the_events_in_order() => _sent.Select(_ => _.Event).ShouldContainOnly(_first, _second);
    [Fact] void should_target_the_triggering_event_source() => _sent.TrueForAll(_ => _.EventSourceId == _eventSourceId).ShouldBeTrue();
    [Fact] void should_key_the_first_event_by_index_zero() => _sent[0].IdempotencyKey.ShouldEqual(_delivery.ReturnedEffectKey(0));
    [Fact] void should_key_the_second_event_by_index_one() => _sent[1].IdempotencyKey.ShouldEqual(_delivery.ReturnedEffectKey(1));
    [Fact] void should_not_use_the_unkeyed_overload() => UnkeyedAppendManyCalls.ShouldEqual(0);
}
