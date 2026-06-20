// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking;

public class handler_method_returning_event_for_event_source_id_with_metadata : Specification
{
    static readonly EventSourceId _targetEventSourceId = EventSourceId.New();
    static readonly EventStreamType _eventStreamType = new("members");
    static readonly EventStreamId _eventStreamId = new("tenant-1");
    static readonly EventSourceType _eventSourceType = new("member");
    static readonly Subject _subject = new("subject-1");
    static readonly DateTimeOffset _occurred = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

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
                return AppendManyResult.Success(CorrelationId.New(), [EventSequenceNumber.First]);
            });

        var sideEffectHandlers = new ReactorSideEffectHandlers(new KnownInstancesOf<IReactorSideEffectHandler>([new EventForEventSourceIdResultHandler()]));
        var reactor = new ReactorWithEventForEventSourceIdWithMetadataReturnType();

        _invoker = new ReactorInvoker(
            eventTypes,
            _middlewares,
            typeof(ReactorWithEventForEventSourceIdWithMetadataReturnType),
            new ActivatedArtifact(reactor, typeof(ReactorWithEventForEventSourceIdWithMetadataReturnType), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>(),
            sideEffectHandlers,
            _eventStore,
            ReactorContextValuesBuilders.ForSpecifications());

        _eventContext = EventContext.EmptyWithEventSourceId(EventSourceId.New());
    }

    async Task Because() => _result = await _invoker.Invoke(new MyEvent(), _eventContext);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_target_the_explicit_event_source_id() => _appended.Single().EventSourceId.ShouldEqual(_targetEventSourceId);
    [Fact] void should_carry_the_event_stream_type() => _appended.Single().EventStreamType.ShouldEqual(_eventStreamType);
    [Fact] void should_carry_the_event_stream_id() => _appended.Single().EventStreamId.ShouldEqual(_eventStreamId);
    [Fact] void should_carry_the_event_source_type() => _appended.Single().EventSourceType.ShouldEqual(_eventSourceType);
    [Fact] void should_carry_the_subject() => _appended.Single().Subject.ShouldEqual(_subject);
    [Fact] void should_carry_the_occurred_time() => _appended.Single().Occurred.ShouldEqual(_occurred);

    class ReactorWithEventForEventSourceIdWithMetadataReturnType : IReactor
    {
        public EventForEventSourceId Handle(MyEvent @event) =>
            new(_targetEventSourceId, new MyOutboundEvent())
            {
                EventStreamType = _eventStreamType,
                EventStreamId = _eventStreamId,
                EventSourceType = _eventSourceType,
                Subject = _subject,
                Occurred = _occurred
            };
    }
}
