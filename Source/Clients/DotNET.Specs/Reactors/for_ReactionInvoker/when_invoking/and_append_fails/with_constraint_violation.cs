// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking.and_append_fails;

public class with_constraint_violation : Specification
{
    ReactorInvocationResult _result;
    IReactorMiddlewares _middlewares;
    ReactorInvoker _invoker;
    IEventStore _eventStore;
    IEventLog _eventLog;
    EventContext _eventContext;
    MyOutboundEvent _outboundEvent;
    AppendResult _failedAppendResult;

    void Establish()
    {
        var eventTypes = new EventTypesForSpecifications([typeof(MyEvent), typeof(MyOutboundEvent)]);
        _middlewares = Substitute.For<IReactorMiddlewares>();
        _outboundEvent = new MyOutboundEvent();

        _eventLog = Substitute.For<IEventLog>();
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.EventLog.Returns(_eventLog);

        // Create a failed AppendResult with constraint violations
        _failedAppendResult = AppendResult.Failed(
            CorrelationId.New(),
            [new ConstraintViolation(
                new EventTypeId("MyOutboundEvent"),
                EventSequenceNumber.Unavailable,
                ConstraintType.Unique,
                "TestConstraint",
                "Constraint violated",
                new ConstraintViolationDetails())]);

        _eventLog.Append(default!, default!, default, default, default, default, default, default, default, default)
            .ReturnsForAnyArgs(_failedAppendResult);

        var sideEffectHandlers = new ReactorSideEffectHandlers(new KnownInstancesOf<IReactorSideEffectHandler>([new EventResultHandler(eventTypes)]));
        var reactor = new ReactorWithSyncEventReturnType(_outboundEvent);

        _invoker = new ReactorInvoker(
            eventTypes,
            _middlewares,
            typeof(ReactorWithSyncEventReturnType),
            new ActivatedArtifact(reactor, typeof(ReactorWithSyncEventReturnType), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>(),
            sideEffectHandlers,
            _eventStore,
            ReactorContextValuesBuilders.ForSpecifications());

        _eventContext = EventContext.EmptyWithEventSourceId(EventSourceId.New());
    }

    async Task Because()
    {
        _result = await _invoker.Invoke(new MyEvent(), _eventContext);
    }

    [Fact] void should_fail() => _result.IsFailed.ShouldBeTrue();
    [Fact] void should_have_side_effect_failure() => _result.SideEffectFailure.ShouldNotBeNull();
    [Fact] void should_have_append_failure() => _result.SideEffectFailure!.AppendFailures.Count().ShouldEqual(1);
    [Fact] void should_have_constraint_violation() => _result.SideEffectFailure!.AppendFailures.First().ConstraintViolations.Count().ShouldEqual(1);
    [Fact] void should_include_constraint_violation_details()
    {
        var violation = _result.SideEffectFailure!.AppendFailures.First().ConstraintViolations.First();
        violation.EventTypeId.ShouldEqual("MyOutboundEvent");
        violation.Message.ShouldEqual("Constraint violated");
    }

    class ReactorWithSyncEventReturnType(MyOutboundEvent outbound) : IReactor
    {
        public MyOutboundEvent Handle(MyEvent @event) => outbound;
    }
}
