// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using CatchResult = Cratis.Monads.Catch;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking;

public class interface_handler_with_derived_events_and_multiple_invokers : Specification
{
    CatchResult _firstResult;
    CatchResult _secondResult;
    TrackingReactor _firstReactor;
    TrackingReactor _secondReactor;
    IReactorMiddlewares _middlewares;
    ReactorInvoker _firstInvoker;
    ReactorInvoker _secondInvoker;

    void Establish()
    {
        var eventTypes = new EventTypesForSpecifications([typeof(MyFirstDerivedEventFromInterface), typeof(MySecondDerivedEventFromInterface)]);
        _middlewares = Substitute.For<IReactorMiddlewares>();
        _firstReactor = new();
        _secondReactor = new();

        _firstInvoker = new ReactorInvoker(
            eventTypes,
            _middlewares,
            typeof(TrackingReactor),
            new ActivatedArtifact(_firstReactor, typeof(TrackingReactor), NullLoggerFactory.Instance),
            Substitute.For<ILogger<ReactorInvoker>>());

        _secondInvoker = new ReactorInvoker(
            eventTypes,
            _middlewares,
            typeof(TrackingReactor),
            new ActivatedArtifact(_secondReactor, typeof(TrackingReactor), NullLoggerFactory.Instance),
            Substitute.For<ILogger<ReactorInvoker>>());
    }

    async Task Because()
    {
        _firstResult = await _firstInvoker.Invoke(new MyFirstDerivedEventFromInterface(), EventContext.Empty);
        _secondResult = await _secondInvoker.Invoke(new MySecondDerivedEventFromInterface(), EventContext.Empty);
    }

    [Fact] void should_succeed_for_the_first_invocation() => _firstResult.TryGetException(out _).ShouldBeFalse();
    [Fact] void should_succeed_for_the_second_invocation() => _secondResult.TryGetException(out _).ShouldBeFalse();
    [Fact] void should_invoke_the_interface_handler_for_the_first_derived_event() => _firstReactor.HandledEventTypes.ShouldContainOnly(typeof(MyFirstDerivedEventFromInterface));
    [Fact] void should_invoke_the_interface_handler_for_the_second_derived_event() => _secondReactor.HandledEventTypes.ShouldContainOnly(typeof(MySecondDerivedEventFromInterface));
    [Fact] void should_call_before_invoke_for_each_event() => _middlewares.Received(2).BeforeInvoke(Arg.Any<EventContext>(), Arg.Any<object>());
    [Fact] void should_call_after_invoke_for_each_event() => _middlewares.Received(2).AfterInvoke(Arg.Any<EventContext>(), Arg.Any<object>());

    class TrackingReactor
    {
        readonly List<Type> _handledEventTypes = [];

        public IReadOnlyCollection<Type> HandledEventTypes => _handledEventTypes;

        public Task Handle(IMyEvent @event)
        {
            _handledEventTypes.Add(@event.GetType());
            return Task.CompletedTask;
        }
    }
}
