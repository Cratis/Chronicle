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
    [Fact] void should_have_reactor_side_effect_exception() => _caughtException.ShouldBeOfExactType<ReactorSideEffectException>();
    [Fact] void should_include_side_effect_failure() => ((ReactorSideEffectException)_caughtException).Failure.ShouldNotBeNull();
    [Fact] void should_have_append_failure() => ((ReactorSideEffectException)_caughtException).Failure.AppendFailures.Count().ShouldEqual(1);
    [Fact] void should_have_errors() => ((ReactorSideEffectException)_caughtException).Failure.AppendFailures.First().Errors.Count().ShouldEqual(1);
    [Fact] void should_include_error_details() => ((ReactorSideEffectException)_caughtException).Failure.AppendFailures.First().Errors.First().ShouldEqual("Database connection failed");

    class ReactorWithTaskOfEventReturnType(MyOutboundEvent outbound) : IReactor
    {
        public Task<MyOutboundEvent> Handle(MyEvent @event) => Task.FromResult(outbound);
    }
}
