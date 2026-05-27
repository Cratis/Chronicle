// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.Logging;
using CatchResult = Cratis.Monads.Catch;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking;

public class sync_handler_method_returning_reactor_side_effect : Specification
{
    CatchResult _result;
    IReactorMiddlewares _middlewares;
    ReactorInvoker _invoker;
    IEventStore _eventStore;
    IEventLog _eventLog;
    EventContext _eventContext;
    MyOutboundEvent _outboundEvent;
    EventSourceId _customEventSourceId;

    void Establish()
    {
        var eventTypes = new EventTypesForSpecifications([typeof(MyEvent), typeof(MyOutboundEvent)]);
        _middlewares = Substitute.For<IReactorMiddlewares>();
        _outboundEvent = new MyOutboundEvent();
        _customEventSourceId = EventSourceId.New();

        _eventLog = Substitute.For<IEventLog>();
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.EventLog.Returns(_eventLog);

        var sideEffectHandlers = new ReactorSideEffectHandlers([new ReactorSideEffectResultHandler()]);
        var reactor = new ReactorWithSyncSideEffectReturnType(_outboundEvent, _customEventSourceId);

        _invoker = new ReactorInvoker(
            eventTypes,
            _middlewares,
            typeof(ReactorWithSyncSideEffectReturnType),
            new ActivatedArtifact(reactor, typeof(ReactorWithSyncSideEffectReturnType), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>(),
            sideEffectHandlers,
            _eventStore);

        _eventContext = EventContext.EmptyWithEventSourceId(EventSourceId.New());
    }

    async Task Because() => _result = await _invoker.Invoke(new MyEvent(), _eventContext);

    [Fact] void should_succeed() => _result.TryGetException(out _).ShouldBeFalse();
    [Fact] void should_append_to_event_log_with_custom_event_source_id() =>
        _eventLog.Received(1).Append(_customEventSourceId, _outboundEvent, default, default, default, default, default, default, default, default);

    class ReactorWithSyncSideEffectReturnType(MyOutboundEvent outbound, EventSourceId eventSourceId) : IReactor
    {
        public ReactorSideEffect Handle(MyEvent @event) =>
            new() { Event = outbound, EventSourceId = eventSourceId };
    }
}
