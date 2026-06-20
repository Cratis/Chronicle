// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking;

public class event_for_event_source_id_is_dispatched_to_the_correct_handler : Specification
{
    ReactorInvocationResult _result;
    IReactorMiddlewares _middlewares;
    ReactorInvoker _invoker;
    IEventStore _eventStore;
    IEventLog _eventLog;
    EventContext _eventContext;
    EventSourceId _targetEventSourceId;
    IEnumerable<EventForEventSourceId> _appended;

    void Establish()
    {
        var eventTypes = new EventTypesForSpecifications([typeof(MyEvent), typeof(MyOutboundEvent)]);
        _middlewares = Substitute.For<IReactorMiddlewares>();
        _targetEventSourceId = EventSourceId.New();

        _eventLog = Substitute.For<IEventLog>();
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.EventLog.Returns(_eventLog);
        _eventLog.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>())
            .ReturnsForAnyArgs(callInfo =>
            {
                _appended = callInfo.Arg<IEnumerable<EventForEventSourceId>>();
                return AppendManyResult.Success(CorrelationId.New(), [EventSequenceNumber.First]);
            });

        // All built-in handlers registered, mirroring the real client and testing harness.
        var sideEffectHandlers = new ReactorSideEffectHandlers(new KnownInstancesOf<IReactorSideEffectHandler>(
        [
            new EventResultHandler(eventTypes),
            new EventsResultHandler(eventTypes),
            new EventForEventSourceIdResultHandler(),
            new EventsForEventSourceIdResultHandler()
        ]));
        var reactor = new ReactorWithEventForEventSourceIdReturnType(_targetEventSourceId);

        _invoker = new ReactorInvoker(
            eventTypes,
            _middlewares,
            typeof(ReactorWithEventForEventSourceIdReturnType),
            new ActivatedArtifact(reactor, typeof(ReactorWithEventForEventSourceIdReturnType), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>(),
            sideEffectHandlers,
            _eventStore,
            ReactorContextValuesBuilders.ForSpecifications());

        _eventContext = EventContext.EmptyWithEventSourceId(EventSourceId.New());
    }

    async Task Because() => _result = await _invoker.Invoke(new MyEvent(), _eventContext);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_through_append_many() => _appended.Single().EventSourceId.ShouldEqual(_targetEventSourceId);
    [Fact] void should_not_append_through_the_single_event_path() => _eventLog.DidNotReceiveWithAnyArgs().Append(default!, default!);

    class ReactorWithEventForEventSourceIdReturnType(EventSourceId eventSourceId) : IReactor
    {
        public EventForEventSourceId Handle(MyEvent @event) => new(eventSourceId, new MyOutboundEvent());
    }
}
