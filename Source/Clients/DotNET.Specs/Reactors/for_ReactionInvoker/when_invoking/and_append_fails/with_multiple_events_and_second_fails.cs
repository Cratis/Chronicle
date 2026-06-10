// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.Logging;
using CatchResult = Cratis.Monads.Catch;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking.and_append_fails;

public class with_multiple_events_and_second_fails : Specification
{
    CatchResult _result;
    IReactorMiddlewares _middlewares;
    ReactorInvoker _invoker;
    IEventStore _eventStore;
    IEventLog _eventLog;
    EventContext _eventContext;
    MyOutboundEvent _firstEvent;
    MyOutboundEvent _secondEvent;
    AppendResult _successAppendResult;
    AppendResult _failedAppendResult;
    Exception _caughtException;

    void Establish()
    {
        var eventTypes = new EventTypesForSpecifications([typeof(MyEvent), typeof(MyOutboundEvent)]);
        _middlewares = Substitute.For<IReactorMiddlewares>();
        _firstEvent = new MyOutboundEvent();
        _secondEvent = new MyOutboundEvent();

        _eventLog = Substitute.For<IEventLog>();
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.EventLog.Returns(_eventLog);

        // Create a successful and a failed AppendResult
        _successAppendResult = AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First);
        _failedAppendResult = AppendResult.Failed(
            CorrelationId.New(),
            [new ConstraintViolation(
                new EventTypeId("MyOutboundEvent"),
                EventSequenceNumber.Unavailable,
                ConstraintType.Unique,
                "TestConstraint",
                "Constraint violated on second event",
                new ConstraintViolationDetails())]);

        // First append succeeds, second fails
        var callCount = 0;
        _eventLog.Append(default!, default!, default, default, default, default, default, default, default, default)
            .ReturnsForAnyArgs(_ => callCount++ == 0 ? _successAppendResult : _failedAppendResult);

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

    async Task Because()
    {
        _result = await _invoker.Invoke(new MyEvent(), _eventContext);
        _result.TryGetException(out _caughtException);
    }

    [Fact] void should_fail() => _result.TryGetException(out _).ShouldBeTrue();
    [Fact] void should_have_reactor_side_effect_exception() => _caughtException.ShouldBeOfExactType<ReactorSideEffectException>();
    [Fact] void should_include_side_effect_failure() => ((ReactorSideEffectException)_caughtException).Failure.ShouldNotBeNull();
    [Fact] void should_have_single_append_failure() => ((ReactorSideEffectException)_caughtException).Failure.AppendFailures.Count().ShouldEqual(1);
    [Fact] void should_have_constraint_violation() => ((ReactorSideEffectException)_caughtException).Failure.AppendFailures.First().ConstraintViolations.Count().ShouldEqual(1);
    [Fact] void should_include_second_event_failure_details()
    {
        var violation = ((ReactorSideEffectException)_caughtException).Failure.AppendFailures.First().ConstraintViolations.First();
        violation.Message.ShouldEqual("Constraint violated on second event");
    }

    class ReactorWithMultipleEventsReturnType(MyOutboundEvent firstEvent, MyOutboundEvent secondEvent) : IReactor
    {
        public IEnumerable<MyOutboundEvent> Handle(MyEvent @event) => [firstEvent, secondEvent];
    }
}
