// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventForEventSourceIdResultHandler.when_handling;

public class and_the_event_carries_its_own_key : given.a_reactor_delivery_with_an_event_log
{
    List<EventForEventSourceId> _sent;
    IdempotencyKey _own;

    async Task Because()
    {
        _own = _delivery.IdempotencyKeyFor("own");
        await new EventForEventSourceIdResultHandler().Handle(_reactorContext, _eventStore, new EventForEventSourceId(EventSourceId.New(), new object()) { IdempotencyKey = _own });
        _sent = [.. KeyedBatches()];
    }

    [Fact] void should_send_the_handlers_key_unchanged() => _sent.Single().IdempotencyKey.ShouldEqual(_own);
}
