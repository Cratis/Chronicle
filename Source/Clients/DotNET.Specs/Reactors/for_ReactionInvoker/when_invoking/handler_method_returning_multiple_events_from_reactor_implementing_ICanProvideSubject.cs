// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking;

public class handler_method_returning_multiple_events_from_reactor_implementing_ICanProvideSubject : Specification
{
    ReactorInvocationResult _result;
    IReactorMiddlewares _middlewares;
    ReactorInvoker _invoker;
    IEventStore _eventStore;
    IEventLog _eventLog;
    EventContext _eventContext;
    MyOutboundEvent _firstEvent;
    MyOutboundEvent _secondEvent;
    Subject _reactorSubject;

    void Establish()
    {
        var eventTypes = new EventTypesForSpecifications([typeof(MyEvent), typeof(MyOutboundEvent)]);
        _middlewares = Substitute.For<IReactorMiddlewares>();
        _firstEvent = new MyOutboundEvent();
        _secondEvent = new MyOutboundEvent();
        _reactorSubject = new Subject("my-subject");

        _eventLog = Substitute.For<IEventLog>();
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.EventLog.Returns(_eventLog);
        _eventLog.AppendMany(default!, default!, default, default, default, default, default, default, default, default)
            .ReturnsForAnyArgs(AppendManyResult.Success(CorrelationId.New(), [EventSequenceNumber.First, EventSequenceNumber.First.Next()]));

        var sideEffectHandlers = new ReactorSideEffectHandlers(new KnownInstancesOf<IReactorSideEffectHandler>([new EventsResultHandler(eventTypes)]));
        var reactor = new ReactorWithSubjectProvider(_firstEvent, _secondEvent, _reactorSubject);

        _invoker = new ReactorInvoker(
            eventTypes,
            _middlewares,
            typeof(ReactorWithSubjectProvider),
            new ActivatedArtifact(reactor, typeof(ReactorWithSubjectProvider), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>(),
            sideEffectHandlers,
            _eventStore,
            ReactorContextValuesBuilders.ForSpecifications());

        _eventContext = EventContext.EmptyWithEventSourceId(EventSourceId.New());
    }

    async Task Because() => _result = await _invoker.Invoke(new MyEvent(), _eventContext);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();

    [Fact]
    void should_append_to_event_log_with_reactor_subject() =>
        _eventLog.Received(1).AppendMany(
            _eventContext.EventSourceId,
            Arg.Any<IEnumerable<object>>(),
            default,
            default,
            default,
            default,
            default,
            default,
            default,
            _reactorSubject);

    class ReactorWithSubjectProvider(MyOutboundEvent firstEvent, MyOutboundEvent secondEvent, Subject subject) : IReactor, ICanProvideSubject
    {
        public IEnumerable<MyOutboundEvent> Handle(MyEvent @event) => [firstEvent, secondEvent];
        public Subject GetSubject() => subject;
    }
}
