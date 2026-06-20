// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking;

public class handler_method_returning_task_of_event_for_event_source_id : Specification
{
    ReactorInvocationResult _result;
    IReactorMiddlewares _middlewares;
    ReactorInvoker _invoker;
    IEventStore _eventStore;
    IEventLog _eventLog;
    EventContext _eventContext;
    EventSourceId _targetEventSourceId;
    MyOutboundEvent _outboundEvent;
    IEnumerable<EventForEventSourceId> _appended;

    void Establish()
    {
        var eventTypes = new EventTypesForSpecifications([typeof(MyEvent), typeof(MyOutboundEvent)]);
        _middlewares = Substitute.For<IReactorMiddlewares>();
        _targetEventSourceId = EventSourceId.New();
        _outboundEvent = new MyOutboundEvent();

        _eventLog = Substitute.For<IEventLog>();
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.EventLog.Returns(_eventLog);
        _eventLog.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>())
            .ReturnsForAnyArgs(callInfo =>
            {
                _appended = callInfo.Arg<IEnumerable<EventForEventSourceId>>();
                return AppendManyResult.Success(CorrelationId.New(), [EventSequenceNumber.First]);
            });

        var sideEffectHandlers = new ReactorSideEffectHandlers(new KnownInstancesOf<IReactorSideEffectHandler>([new EventForEventSourceIdResultHandler()]));
        var reactor = new ReactorWithTaskOfEventForEventSourceIdReturnType(_targetEventSourceId, _outboundEvent);

        _invoker = new ReactorInvoker(
            eventTypes,
            _middlewares,
            typeof(ReactorWithTaskOfEventForEventSourceIdReturnType),
            new ActivatedArtifact(reactor, typeof(ReactorWithTaskOfEventForEventSourceIdReturnType), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>(),
            sideEffectHandlers,
            _eventStore,
            ReactorContextValuesBuilders.ForSpecifications());

        _eventContext = EventContext.EmptyWithEventSourceId(EventSourceId.New());
    }

    async Task Because() => _result = await _invoker.Invoke(new MyEvent(), _eventContext);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_a_single_event() => _appended.Count().ShouldEqual(1);
    [Fact] void should_target_the_explicit_event_source_id() => _appended.Single().EventSourceId.ShouldEqual(_targetEventSourceId);
    [Fact] void should_append_the_returned_event() => _appended.Single().Event.ShouldEqual(_outboundEvent);

    class ReactorWithTaskOfEventForEventSourceIdReturnType(EventSourceId eventSourceId, MyOutboundEvent outbound) : IReactor
    {
        public Task<EventForEventSourceId> Handle(MyEvent @event) => Task.FromResult(new EventForEventSourceId(eventSourceId, outbound));
    }
}
