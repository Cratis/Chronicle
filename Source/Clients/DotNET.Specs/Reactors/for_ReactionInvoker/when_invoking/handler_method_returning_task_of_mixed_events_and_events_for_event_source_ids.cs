// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking;

public class handler_method_returning_task_of_mixed_events_and_events_for_event_source_ids : Specification
{
    static readonly EventSourceId _explicitEventSourceId = EventSourceId.New();

    ReactorInvocationResult _result;
    IReactorMiddlewares _middlewares;
    ReactorInvoker _invoker;
    IEventStore _eventStore;
    IEventLog _eventLog;
    EventContext _eventContext;
    IEnumerable<EventForEventSourceId> _appended;

    void Establish()
    {
        var eventTypes = new EventTypesForSpecifications([typeof(MyEvent), typeof(MyOutboundEvent)]);
        _middlewares = Substitute.For<IReactorMiddlewares>();

        _eventLog = Substitute.For<IEventLog>();
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.EventLog.Returns(_eventLog);
        _eventLog.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>())
            .ReturnsForAnyArgs(callInfo =>
            {
                _appended = callInfo.Arg<IEnumerable<EventForEventSourceId>>();
                return AppendManyResult.Success(CorrelationId.New(), [EventSequenceNumber.First, new EventSequenceNumber(1)]);
            });

        var sideEffectHandlers = new ReactorSideEffectHandlers(new KnownInstancesOf<IReactorSideEffectHandler>([new MixedSideEffectsResultHandler(eventTypes)]));
        var reactor = new ReactorWithTaskOfMixedSideEffectsReturnType();

        _invoker = new ReactorInvoker(
            eventTypes,
            _middlewares,
            typeof(ReactorWithTaskOfMixedSideEffectsReturnType),
            new ActivatedArtifact(reactor, typeof(ReactorWithTaskOfMixedSideEffectsReturnType), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>(),
            sideEffectHandlers,
            _eventStore,
            ReactorContextValuesBuilders.ForSpecifications());

        _eventContext = EventContext.EmptyWithEventSourceId(EventSourceId.New());
    }

    async Task Because() => _result = await _invoker.Invoke(new MyEvent(), _eventContext);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_both_side_effects() => _appended.Count().ShouldEqual(2);
    [Fact] void should_append_the_bare_event_to_the_triggering_event_source_id() => _appended.First().EventSourceId.ShouldEqual(_eventContext.EventSourceId);
    [Fact] void should_append_the_event_for_event_source_id_to_its_explicit_id() => _appended.Last().EventSourceId.ShouldEqual(_explicitEventSourceId);

    class ReactorWithTaskOfMixedSideEffectsReturnType : IReactor
    {
        public Task<IEnumerable<object>> Handle(MyEvent @event) =>
            Task.FromResult<IEnumerable<object>>(
            [
                new MyOutboundEvent(),
                new EventForEventSourceId(_explicitEventSourceId, new MyOutboundEvent())
            ]);
    }
}
