// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking;

public class and_sync_handler_methods_return_event_sequences : Specification
{
    ReactorInvocationResult _arrayResult;
    ReactorInvocationResult _listResult;
    ReactorInvoker _arrayInvoker;
    ReactorInvoker _listInvoker;
    IEventLog _eventLog;
    MyOutboundEvent _outboundEvent;
    EventContext _eventContext;

    void Establish()
    {
        _outboundEvent = new MyOutboundEvent();
        var eventTypes = new EventTypesForSpecifications([typeof(MyEvent), typeof(MyOutboundEvent)]);
        var eventStore = Substitute.For<IEventStore>();
        eventStore.EventTypes.Returns(eventTypes);
        _eventLog = Substitute.For<IEventLog>();
        eventStore.EventLog.Returns(_eventLog);
        _eventLog.AppendMany(default!, default!, default, default, default, default, default, default, default, default)
            .ReturnsForAnyArgs(AppendManyResult.Success(CorrelationId.New(), [EventSequenceNumber.First]));

        var handlers = new ReactorSideEffectHandlers(new KnownInstancesOf<IReactorSideEffectHandler>([new EventsResultHandler()]));
        _arrayInvoker = CreateInvoker(new ArrayReactor(_outboundEvent), eventTypes, eventStore, handlers);
        _listInvoker = CreateInvoker(new ListReactor(_outboundEvent), eventTypes, eventStore, handlers);
        _eventContext = EventContext.EmptyWithEventSourceId(EventSourceId.New());
    }

    async Task Because()
    {
        _arrayResult = await _arrayInvoker.Invoke(new MyEvent(), _eventContext);
        _listResult = await _listInvoker.Invoke(new MyEvent(), _eventContext);
    }

    [Fact] void should_succeed_for_an_array() => _arrayResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_succeed_for_a_list() => _listResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_both_sequences() => _eventLog.Received(2).AppendMany(_eventContext.EventSourceId, Arg.Is<IEnumerable<object>>(events => events.SequenceEqual(new[] { _outboundEvent })), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<Cratis.Chronicle.EventSequences.Concurrency.ConcurrencyScope?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<Subject?>());

    static ReactorInvoker CreateInvoker(IReactor reactor, IEventTypes eventTypes, IEventStore eventStore, IReactorSideEffectHandlers handlers) =>
        new(
            eventTypes,
            Substitute.For<IReactorMiddlewares>(),
            reactor.GetType(),
            new ActivatedArtifact(reactor, reactor.GetType(), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>(),
            handlers,
            eventStore,
            ReactorContextValuesBuilders.ForSpecifications());

    class ArrayReactor(MyOutboundEvent outboundEvent) : IReactor
    {
        public MyOutboundEvent[] Handle(MyEvent @event) => [outboundEvent];
    }

    class ListReactor(MyOutboundEvent outboundEvent) : IReactor
    {
        public List<MyOutboundEvent> Handle(MyEvent @event) => [outboundEvent];
    }
}
