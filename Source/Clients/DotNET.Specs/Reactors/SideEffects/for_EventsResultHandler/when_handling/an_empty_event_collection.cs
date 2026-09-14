// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Monads;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventsResultHandler.when_handling;

public class an_empty_event_collection : Specification
{
    EventsResultHandler _handler;
    IEventStore _eventStore;
    Result<ReactorSideEffectFailure> _result;

    void Establish()
    {
        _handler = new();
        _eventStore = Substitute.For<IEventStore>();
    }

    async Task Because() => _result = await _handler.Handle(
        new ReactorContext(EventContext.Empty, new object(), ReactorContextValues.Empty),
        _eventStore,
        Array.Empty<object>());

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_append_an_empty_batch() => _eventStore.EventLog.DidNotReceiveWithAnyArgs().AppendMany(default!, default!, default, default, default, default, default, default, default, default);
}
