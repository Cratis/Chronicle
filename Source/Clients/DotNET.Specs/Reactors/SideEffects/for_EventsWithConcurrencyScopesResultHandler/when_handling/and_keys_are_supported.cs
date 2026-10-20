// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventsWithConcurrencyScopesResultHandler.when_handling;

public class and_keys_are_supported : given.a_reactor_delivery_with_an_event_log
{
    List<EventForEventSourceId> _sent;
    IDictionary<EventSourceId, ConcurrencyScope> _scopes;
    EventForEventSourceId _event;

    void Establish()
    {
        _event = new(EventSourceId.New(), new object());
        _eventLog.AppendMany(default!, default, default, default)
            .ReturnsForAnyArgs(callInfo =>
            {
                _scopes = callInfo.ArgAt<IDictionary<EventSourceId, ConcurrencyScope>>(3);
                return AppendManyResult.Success(CorrelationId.New(), [EventSequenceNumber.First]);
            });
    }

    async Task Because()
    {
        await new EventsWithConcurrencyScopesResultHandler().Handle(
            _reactorContext,
            _eventStore,
            new EventsWithConcurrencyScopes([_event], [new(_event.EventSourceId, ConcurrencyScope.NotSet)]));
        _sent = [.. KeyedBatches()];
    }

    [Fact] void should_key_the_event_by_its_index() => _sent.Single().IdempotencyKey.ShouldEqual(_delivery.ReturnedEffectKey(0));
    [Fact] void should_still_send_the_scopes() => _scopes.Keys.ShouldContainOnly(_event.EventSourceId);
}
