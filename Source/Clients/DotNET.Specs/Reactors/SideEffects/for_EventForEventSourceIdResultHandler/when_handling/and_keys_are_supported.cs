// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventForEventSourceIdResultHandler.when_handling;

public class and_keys_are_supported : given.a_reactor_delivery_with_an_event_log
{
    protected override bool KernelSupportsIdempotentAppends => true;

    List<EventForEventSourceId> _sent;
    Result<ReactorSideEffectFailure> _result;

    async Task Because()
    {
        _result = await new EventForEventSourceIdResultHandler().Handle(_reactorContext, _eventStore, new EventForEventSourceId(EventSourceId.New(), new object()));
        _sent = [.. KeyedBatches()];
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_key_it_by_index_zero() => _sent.Single().IdempotencyKey.ShouldEqual(_delivery.ReturnedEffectKey(0));
}
