// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking.and_append_fails;

public class with_mixed_side_effects : Specification
{
    static readonly EventSourceId _explicitEventSourceId = EventSourceId.New();

    ReactorInvocationResult _result;
    IReactorMiddlewares _middlewares;
    ReactorInvoker _invoker;
    IEventStore _eventStore;
    IEventLog _eventLog;
    EventContext _eventContext;
    AppendManyResult _failedAppendResult;
    EventSourceId _contextEventSourceId;

    void Establish()
    {
        var eventTypes = new EventTypesForSpecifications([typeof(MyEvent), typeof(MyOutboundEvent)]);
        _middlewares = Substitute.For<IReactorMiddlewares>();
        _contextEventSourceId = EventSourceId.New();

        _eventLog = Substitute.For<IEventLog>();
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.EventLog.Returns(_eventLog);
        _eventStore.EventTypes.Returns(eventTypes);

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

        var sideEffectHandlers = new ReactorSideEffectHandlers(new KnownInstancesOf<IReactorSideEffectHandler>([new MixedSideEffectsResultHandler()]));
        var reactor = new ReactorWithMixedSideEffectsReturnType();

        _invoker = new ReactorInvoker(
            eventTypes,
            _middlewares,
            typeof(ReactorWithMixedSideEffectsReturnType),
            new ActivatedArtifact(reactor, typeof(ReactorWithMixedSideEffectsReturnType), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>(),
            sideEffectHandlers,
            _eventStore,
            ReactorContextValuesBuilders.ForSpecifications());

        _eventContext = EventContext.EmptyWithEventSourceId(_contextEventSourceId);
    }

    async Task Because() => _result = await _invoker.Invoke(new MyEvent(), _eventContext);

    [Fact] void should_fail() => _result.IsFailed.ShouldBeTrue();
    [Fact] void should_have_side_effect_failure() => _result.SideEffectFailure.ShouldNotBeNull();
    [Fact] void should_capture_both_target_event_source_ids() =>
        _result.SideEffectFailure!.GetTargetEventSourceIds().Count().ShouldEqual(2);
    [Fact] void should_capture_the_bare_events_triggering_event_source_id() =>
        _result.SideEffectFailure!.GetTargetEventSourceIds().ShouldContain(_contextEventSourceId);
    [Fact] void should_capture_the_explicit_target_event_source_id() =>
        _result.SideEffectFailure!.GetTargetEventSourceIds().ShouldContain(_explicitEventSourceId);

    class ReactorWithMixedSideEffectsReturnType : IReactor
    {
        public IEnumerable<object> Handle(MyEvent @event) =>
        [
            new MyOutboundEvent(),
            new EventForEventSourceId(_explicitEventSourceId, new MyOutboundEvent())
        ];
    }
}
