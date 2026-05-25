// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Microsoft.Extensions.Logging;
using CatchResult = Cratis.Monads.Catch;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking;

public class handler_method_returning_multiple_reactor_side_effects : Specification
{
    CatchResult _result;
    IReactorMiddlewares _middlewares;
    ReactorInvoker _invoker;
    IEventStore _eventStore;
    IEventLog _eventLog;
    EventContext _eventContext;
    MyOutboundEvent _firstEvent;
    MyOutboundEvent _secondEvent;
    EventSourceId _firstEventSourceId;
    EventSourceId _secondEventSourceId;

    void Establish()
    {
        var eventTypes = new EventTypesForSpecifications([typeof(MyEvent), typeof(MyOutboundEvent)]);
        _middlewares = Substitute.For<IReactorMiddlewares>();
        _firstEvent = new MyOutboundEvent();
        _secondEvent = new MyOutboundEvent();
        _firstEventSourceId = EventSourceId.New();
        _secondEventSourceId = EventSourceId.New();

        _eventLog = Substitute.For<IEventLog>();
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.EventLog.Returns(_eventLog);

        var reactor = new ReactorWithMultipleSideEffectsReturnType(_firstEvent, _firstEventSourceId, _secondEvent, _secondEventSourceId);

        _invoker = new ReactorInvoker(
            eventTypes,
            _middlewares,
            typeof(ReactorWithMultipleSideEffectsReturnType),
            new ActivatedArtifact(reactor, typeof(ReactorWithMultipleSideEffectsReturnType), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>(),
            _eventStore);

        _eventContext = EventContext.EmptyWithEventSourceId(EventSourceId.New());
    }

    async Task Because() => _result = await _invoker.Invoke(new MyEvent(), _eventContext);

    [Fact] void should_succeed() => _result.TryGetException(out _).ShouldBeFalse();
    [Fact] void should_append_first_event() =>
        _eventLog.Received(1).Append(_firstEventSourceId, _firstEvent, default, default, default, default, default, default, default, default);
    [Fact] void should_append_second_event() =>
        _eventLog.Received(1).Append(_secondEventSourceId, _secondEvent, default, default, default, default, default, default, default, default);

    class ReactorWithMultipleSideEffectsReturnType(
        MyOutboundEvent firstEvent,
        EventSourceId firstEventSourceId,
        MyOutboundEvent secondEvent,
        EventSourceId secondEventSourceId) : IReactor
    {
        public Task<IEnumerable<ReactorSideEffect>> Handle(MyEvent @event) =>
            Task.FromResult<IEnumerable<ReactorSideEffect>>(
            [
                new ReactorSideEffect { Event = firstEvent, EventSourceId = firstEventSourceId },
                new ReactorSideEffect { Event = secondEvent, EventSourceId = secondEventSourceId }
            ]);
    }
}
