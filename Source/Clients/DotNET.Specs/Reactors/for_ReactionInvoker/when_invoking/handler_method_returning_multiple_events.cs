// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking;

public class handler_method_returning_multiple_events : Specification
{
    ReactorInvocationResult _result;
    IReactorMiddlewares _middlewares;
    ReactorInvoker _invoker;
    IEventStore _eventStore;
    IEventLog _eventLog;
    EventContext _eventContext;
    MyOutboundEvent _firstEvent;
    MyOutboundEvent _secondEvent;
    IEnumerable<object> _eventsAppended;

    void Establish()
    {
        var eventTypes = new EventTypesForSpecifications([typeof(MyEvent), typeof(MyOutboundEvent)]);
        _middlewares = Substitute.For<IReactorMiddlewares>();
        _firstEvent = new MyOutboundEvent();
        _secondEvent = new MyOutboundEvent();

        _eventLog = Substitute.For<IEventLog>();
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.EventLog.Returns(_eventLog);
        _eventLog.AppendMany(default!, default!, default, default, default, default, default, default, default, default)
            .ReturnsForAnyArgs(callInfo =>
            {
                _eventsAppended = callInfo.ArgAt<IEnumerable<object>>(1);
                return AppendManyResult.Success(CorrelationId.New(), [EventSequenceNumber.First, EventSequenceNumber.First.Next()]);
            });

        var sideEffectHandlers = new ReactorSideEffectHandlers(new KnownInstancesOf<IReactorSideEffectHandler>([new EventsResultHandler(eventTypes)]));
        var reactor = new ReactorWithMultipleEventsReturnType(_firstEvent, _secondEvent);

        _invoker = new ReactorInvoker(
            eventTypes,
            _middlewares,
            typeof(ReactorWithMultipleEventsReturnType),
            new ActivatedArtifact(reactor, typeof(ReactorWithMultipleEventsReturnType), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>(),
            sideEffectHandlers,
            _eventStore,
            ReactorContextValuesBuilders.ForSpecifications());

        _eventContext = EventContext.EmptyWithEventSourceId(EventSourceId.New());
    }

    async Task Because() => _result = await _invoker.Invoke(new MyEvent(), _eventContext);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_events_as_one_operation() =>
        _eventLog.Received(1).AppendMany(_eventContext.EventSourceId, Arg.Any<IEnumerable<object>>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<ConcurrencyScope?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<Subject?>());

    [Fact] void should_append_both_events_to_the_triggering_event_source_id() =>
        _eventsAppended.ShouldContainOnly(_firstEvent, _secondEvent);

    [Fact] void should_not_append_events_individually() =>
        _eventLog.DidNotReceiveWithAnyArgs().Append(default!, default!, default, default, default, default, default, default, default, default);

    class ReactorWithMultipleEventsReturnType(MyOutboundEvent firstEvent, MyOutboundEvent secondEvent) : IReactor
    {
        public IEnumerable<MyOutboundEvent> Handle(MyEvent @event) => [firstEvent, secondEvent];
    }
}
