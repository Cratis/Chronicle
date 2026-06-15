// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking;

public class mixed_bare_event_inherits_reactor_metadata_while_event_for_event_source_id_keeps_its_own : Specification
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
        var reactor = new ReactorWithEventStreamTypeAttributeReturningMixed();

        _invoker = new ReactorInvoker(
            eventTypes,
            _middlewares,
            typeof(ReactorWithEventStreamTypeAttributeReturningMixed),
            new ActivatedArtifact(reactor, typeof(ReactorWithEventStreamTypeAttributeReturningMixed), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>(),
            sideEffectHandlers,
            _eventStore,
            ReactorContextValuesBuilders.ForSpecifications());

        _eventContext = EventContext.EmptyWithEventSourceId(EventSourceId.New());
    }

    async Task Because() => _result = await _invoker.Invoke(new MyEvent(), _eventContext);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_apply_the_reactor_stream_type_to_the_bare_event() => _appended.First().EventStreamType.ShouldEqual(new EventStreamType("reactor-stream"));
    [Fact] void should_leave_the_event_for_event_source_id_with_its_own_default_stream_type() => _appended.Last().EventStreamType.ShouldEqual(EventStreamType.All);

    [EventStreamType("reactor-stream")]
    class ReactorWithEventStreamTypeAttributeReturningMixed : IReactor
    {
        public IEnumerable<object> Handle(MyEvent @event) =>
        [
            new MyOutboundEvent(),
            new EventForEventSourceId(_explicitEventSourceId, new MyOutboundEvent())
        ];
    }
}
