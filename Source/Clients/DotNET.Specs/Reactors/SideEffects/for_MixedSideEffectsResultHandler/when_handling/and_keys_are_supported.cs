// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.SideEffects.for_MixedSideEffectsResultHandler.when_handling;

public class and_keys_are_supported : given.a_reactor_delivery_with_an_event_log
{
    List<EventForEventSourceId> _sent;
    IdempotencyKey _own;

    async Task Because()
    {
        _own = _delivery.IdempotencyKeyFor("own");
        object[] returned =
        [
            new object(),
            new EventForEventSourceId(EventSourceId.New(), new object()) { IdempotencyKey = _own },
            new EventForEventSourceId(EventSourceId.New(), new object())
        ];
        await new MixedSideEffectsResultHandler().Handle(_reactorContext, _eventStore, returned);
        _sent = [.. KeyedBatches()];
    }

    [Fact] void should_key_a_plain_event_by_its_index() => _sent[0].IdempotencyKey.ShouldEqual(_delivery.ReturnedEffectKey(0));
    [Fact] void should_keep_the_key_the_handler_set() => _sent[1].IdempotencyKey.ShouldEqual(_own);
    [Fact] void should_key_an_unkeyed_targeted_event_by_its_index() => _sent[2].IdempotencyKey.ShouldEqual(_delivery.ReturnedEffectKey(2));
}
