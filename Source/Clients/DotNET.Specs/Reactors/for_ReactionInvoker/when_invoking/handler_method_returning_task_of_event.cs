// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking;

public class handler_method_returning_task_of_event : Specification
{
    ReactorInvocationResult _result;
    IReactorMiddlewares _middlewares;
    ReactorInvoker _invoker;
    IEventStore _eventStore;
    IEventLog _eventLog;
    EventContext _eventContext;
    MyOutboundEvent _outboundEvent;

    void Establish()
    {
        var eventTypes = new EventTypesForSpecifications([typeof(MyEvent), typeof(MyOutboundEvent)]);
        _middlewares = Substitute.For<IReactorMiddlewares>();
        _outboundEvent = new MyOutboundEvent();

        _eventLog = Substitute.For<IEventLog>();
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.EventLog.Returns(_eventLog);
        _eventLog.Append(default!, default!, default, default, default, default, default, default, default, default)
            .ReturnsForAnyArgs(AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First));

        var sideEffectHandlers = new ReactorSideEffectHandlers(new KnownInstancesOf<IReactorSideEffectHandler>([new EventResultHandler(eventTypes)]));
        var reactor = new ReactorWithTaskOfEventReturnType(_outboundEvent);

        _invoker = new ReactorInvoker(
            eventTypes,
            _middlewares,
            typeof(ReactorWithTaskOfEventReturnType),
            new ActivatedArtifact(reactor, typeof(ReactorWithTaskOfEventReturnType), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>(),
            sideEffectHandlers,
            _eventStore,
            ReactorContextValuesBuilders.ForSpecifications());

        _eventContext = EventContext.EmptyWithEventSourceId(EventSourceId.New());
    }

    async Task Because() => _result = await _invoker.Invoke(new MyEvent(), _eventContext);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_to_event_log() => _eventLog.Received(1).Append(_eventContext.EventSourceId, _outboundEvent, default, default, default, default, default, default, default, default);

    class ReactorWithTaskOfEventReturnType(MyOutboundEvent outbound) : IReactor
    {
        public Task<MyOutboundEvent> Handle(MyEvent @event) => Task.FromResult(outbound);
    }
}
