// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventsForEventSourceIdResultHandler.when_handling;

public class and_one_event_carries_its_own_key : given.a_reactor_delivery_with_an_event_log
{
    List<EventForEventSourceId> _sent;
    IdempotencyKey _own;

    async Task Because()
    {
        _own = _delivery.IdempotencyKeyFor("own");
        await new EventsForEventSourceIdResultHandler().Handle(_reactorContext, _eventStore, new[]
        {
            new EventForEventSourceId(EventSourceId.New(), new object()),
            new EventForEventSourceId(EventSourceId.New(), new object()) { IdempotencyKey = _own },
            new EventForEventSourceId(EventSourceId.New(), new object())
        });
        _sent = [.. KeyedBatches()];
    }

    [Fact] void should_key_the_first_by_its_index() => _sent[0].IdempotencyKey.ShouldEqual(_delivery.ReturnedEffectKey(0));
    [Fact] void should_keep_the_key_the_handler_set() => _sent[1].IdempotencyKey.ShouldEqual(_own);
    [Fact] void should_key_the_third_by_its_index() => _sent[2].IdempotencyKey.ShouldEqual(_delivery.ReturnedEffectKey(2));
}
