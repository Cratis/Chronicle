// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.Logging;
using CatchResult = Cratis.Monads.Catch;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking.and_append_fails;

public class with_append_error : Specification
{
    CatchResult _result;
    IReactorMiddlewares _middlewares;
    ReactorInvoker _invoker;
    IEventStore _eventStore;
    IEventLog _eventLog;
    EventContext _eventContext;
    MyOutboundEvent _outboundEvent;
    AppendResult _failedAppendResult;
    Exception _caughtException;

    void Establish()
    {
        var eventTypes = new EventTypesForSpecifications([typeof(MyEvent), typeof(MyOutboundEvent)]);
        _middlewares = Substitute.For<IReactorMiddlewares>();
        _outboundEvent = new MyOutboundEvent();

        _eventLog = Substitute.For<IEventLog>();
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.EventLog.Returns(_eventLog);

        // Create a failed AppendResult with errors
        _failedAppendResult = AppendResult.Failed(
            CorrelationId.New(),
            [new AppendError("Database connection failed")]);

        _eventLog.Append(default!, default!, default, default, default, default, default, default, default, default)
            .ReturnsForAnyArgs(_failedAppendResult);

        var sideEffectHandlers = new ReactorSideEffectHandlers(new KnownInstancesOf<IReactorSideEffectHandler>([new EventResultHandler(eventTypes)]));
        var reactor = new ReactorWithTaskOfEventReturnType(_outboundEvent);

        _invoker = new ReactorInvoker(
            eventTypes,
            _middlewares,
            typeof(ReactorWithTaskOfEventReturnType),
            new ActivatedArtifact(reactor, typeof(ReactorWithTaskOfEventReturnType), Substitute.For<ILogger<ActivatedArtifact>>()),
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
    [Fact] void should_have_reactor_append_failed_exception() => _caughtException.ShouldBeOfExactType<ReactorAppendFailedException>();
    [Fact] void should_include_append_result() => ((ReactorAppendFailedException)_caughtException).AppendResult.ShouldEqual(_failedAppendResult);
    [Fact] void should_include_errors_in_message() => _caughtException.Message.ShouldContain("Errors");
    [Fact] void should_include_error_details_in_message() => _caughtException.Message.ShouldContain("Database connection failed");

    class ReactorWithTaskOfEventReturnType(MyOutboundEvent outbound) : IReactor
    {
        public Task<MyOutboundEvent> Handle(MyEvent @event) => Task.FromResult(outbound);
    }
}
