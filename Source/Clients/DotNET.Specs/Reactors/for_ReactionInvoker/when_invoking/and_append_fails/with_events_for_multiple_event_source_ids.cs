// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking.and_append_fails;

public class with_events_for_multiple_event_source_ids : Specification
{
    ReactorInvocationResult _result;
    IReactorMiddlewares _middlewares;
    ReactorInvoker _invoker;
    IEventStore _eventStore;
    IEventLog _eventLog;
    EventContext _eventContext;
    AppendManyResult _failedAppendResult;
    EventSourceId _firstEventSourceId;
    EventSourceId _secondEventSourceId;

    void Establish()
    {
        var eventTypes = new EventTypesForSpecifications([typeof(MyEvent), typeof(MyOutboundEvent)]);
        _middlewares = Substitute.For<IReactorMiddlewares>();
        _firstEventSourceId = EventSourceId.New();
        _secondEventSourceId = EventSourceId.New();

        _eventLog = Substitute.For<IEventLog>();
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.EventLog.Returns(_eventLog);

        _failedAppendResult = AppendManyResult.Failed(
            CorrelationId.New(),
            [new ConstraintViolation(
                new EventTypeId("MyOutboundEvent"),
                EventSequenceNumber.Unavailable,
                ConstraintType.Unique,
                "TestConstraint",
                "Constraint violated",
                new ConstraintViolationDetails())]);

        _eventLog.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>())
            .ReturnsForAnyArgs(_failedAppendResult);

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

    [Fact] void should_fail() => _result.IsFailed.ShouldBeTrue();
    [Fact] void should_have_side_effect_failure() => _result.SideEffectFailure.ShouldNotBeNull();
    [Fact] void should_capture_both_target_event_source_ids() =>
        _result.SideEffectFailure!.GetTargetEventSourceIds().Count().ShouldEqual(2);
    [Fact] void should_capture_the_first_target_event_source_id() =>
        _result.SideEffectFailure!.GetTargetEventSourceIds().ShouldContain(_firstEventSourceId);
    [Fact] void should_capture_the_second_target_event_source_id() =>
        _result.SideEffectFailure!.GetTargetEventSourceIds().ShouldContain(_secondEventSourceId);

    class ReactorWithEventsForEventSourceIdsReturnType(EventSourceId firstEventSourceId, EventSourceId secondEventSourceId) : IReactor
    {
        public IEnumerable<EventForEventSourceId> Handle(MyEvent @event) =>
        [
            new(firstEventSourceId, new MyOutboundEvent()),
            new(secondEventSourceId, new MyOutboundEvent())
        ];
    }
}
