// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking;

public class handler_method_returning_events_for_event_source_ids : Specification
{
    ReactorInvocationResult _result;
    IReactorMiddlewares _middlewares;
    ReactorInvoker _invoker;
    IEventStore _eventStore;
    IEventLog _eventLog;
    EventContext _eventContext;
    EventSourceId _firstEventSourceId;
    EventSourceId _secondEventSourceId;
    IEnumerable<EventForEventSourceId> _appended;

    void Establish()
    {
        var eventTypes = new EventTypesForSpecifications([typeof(MyEvent), typeof(MyOutboundEvent)]);
        _middlewares = Substitute.For<IReactorMiddlewares>();
        _firstEventSourceId = EventSourceId.New();
        _secondEventSourceId = EventSourceId.New();

        _eventLog = Substitute.For<IEventLog>();
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.EventLog.Returns(_eventLog);
        _eventLog.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>())
            .ReturnsForAnyArgs(callInfo =>
            {
                _appended = callInfo.Arg<IEnumerable<EventForEventSourceId>>();
                return AppendManyResult.Success(CorrelationId.New(), [EventSequenceNumber.First, new EventSequenceNumber(1)]);
            });

        var sideEffectHandlers = new ReactorSideEffectHandlers(new KnownInstancesOf<IReactorSideEffectHandler>([new EventsForEventSourceIdResultHandler()]));
        var reactor = new ReactorWithEventsForEventSourceIdsReturnType(_firstEventSourceId, _secondEventSourceId);

        _invoker = new ReactorInvoker(
            eventTypes,
            _middlewares,
            typeof(ReactorWithEventsForEventSourceIdsReturnType),
            new ActivatedArtifact(reactor, typeof(ReactorWithEventsForEventSourceIdsReturnType), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>(),
            sideEffectHandlers,
            _eventStore,
            ReactorContextValuesBuilders.ForSpecifications());

        _eventContext = EventContext.EmptyWithEventSourceId(EventSourceId.New());
    }

    async Task Because() => _result = await _invoker.Invoke(new MyEvent(), _eventContext);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_all_events() => _appended.Count().ShouldEqual(2);
    [Fact] void should_target_the_first_event_source_id() => _appended.First().EventSourceId.ShouldEqual(_firstEventSourceId);
    [Fact] void should_target_the_second_event_source_id() => _appended.Last().EventSourceId.ShouldEqual(_secondEventSourceId);

    class ReactorWithEventsForEventSourceIdsReturnType(EventSourceId firstEventSourceId, EventSourceId secondEventSourceId) : IReactor
    {
        public IEnumerable<EventForEventSourceId> Handle(MyEvent @event) =>
        [
            new(firstEventSourceId, new MyOutboundEvent()),
            new(secondEventSourceId, new MyOutboundEvent())
        ];
    }
}
