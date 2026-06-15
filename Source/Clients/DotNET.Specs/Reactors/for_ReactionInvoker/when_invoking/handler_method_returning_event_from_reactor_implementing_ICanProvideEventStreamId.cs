// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking;

public class handler_method_returning_event_from_reactor_implementing_ICanProvideEventStreamId : Specification
{
    ReactorInvocationResult _result;
    IReactorMiddlewares _middlewares;
    ReactorInvoker _invoker;
    IEventStore _eventStore;
    IEventLog _eventLog;
    EventContext _eventContext;
    MyOutboundEvent _outboundEvent;
    EventStreamId _reactorEventStreamId;

    void Establish()
    {
        var eventTypes = new EventTypesForSpecifications([typeof(MyEvent), typeof(MyOutboundEvent)]);
        _middlewares = Substitute.For<IReactorMiddlewares>();
        _outboundEvent = new MyOutboundEvent();
        _reactorEventStreamId = new EventStreamId("my-stream-instance");

        _eventLog = Substitute.For<IEventLog>();
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.EventLog.Returns(_eventLog);

        var sideEffectHandlers = new ReactorSideEffectHandlers(new KnownInstancesOf<IReactorSideEffectHandler>([new EventResultHandler(eventTypes)]));
        var reactor = new ReactorWithCustomEventStreamId(_outboundEvent, _reactorEventStreamId);

        _invoker = new ReactorInvoker(
            eventTypes,
            _middlewares,
            typeof(ReactorWithCustomEventStreamId),
            new ActivatedArtifact(reactor, typeof(ReactorWithCustomEventStreamId), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>(),
            sideEffectHandlers,
            _eventStore,
            ReactorContextValuesBuilders.ForSpecifications());

        _eventContext = EventContext.EmptyWithEventSourceId(EventSourceId.New());
    }

    async Task Because() => _result = await _invoker.Invoke(new MyEvent(), _eventContext);

    [Fact] void should_succeed() => _result.ExceptionResult.TryGetException(out _).ShouldBeFalse();
    [Fact] void should_append_to_event_log_with_reactor_event_stream_id() =>
        _eventLog.Received(1).Append(_eventContext.EventSourceId, _outboundEvent, default, _reactorEventStreamId, default, default, default, default, default, default);

    class ReactorWithCustomEventStreamId(MyOutboundEvent outbound, EventStreamId eventStreamId) : IReactor, ICanProvideEventStreamId
    {
        public Task<MyOutboundEvent> Handle(MyEvent @event) => Task.FromResult(outbound);
        public EventStreamId GetEventStreamId() => eventStreamId;
    }
}
