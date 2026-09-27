// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorSideEffectHandlers;

public class when_resolution_fails_after_an_appending_handler : Specification
{
    ReactorSideEffectHandlers _handlers;
    IReactorSideEffectHandler _first;
    IEventStore _eventStore;
    Exception _error;

    void Establish()
    {
        _first = Substitute.For<IReactorSideEffectHandler>();
        _first.CanHandle(Arg.Any<ReactorContext>(), Arg.Any<IEventStore>(), Arg.Any<object>()).Returns(true);
        _first.Handle(Arg.Any<ReactorContext>(), Arg.Any<IEventStore>(), Arg.Any<object>())
            .Returns(Task.FromResult(Cratis.Monads.Result.Success<ReactorSideEffectFailure>()));
        _handlers = new ReactorSideEffectHandlers(new FailingInstances(_first));
        _eventStore = Substitute.For<IEventStore>();
    }

    async Task Because() => _error = await Catch.Exception(() => _handlers.Handle(
        new ReactorContext(EventContext.Empty, new object(), ReactorContextValues.Empty), _eventStore, new object()));

    [Fact] void should_fail_before_invoking_the_first_handler() => _first.DidNotReceive().Handle(Arg.Any<ReactorContext>(), Arg.Any<IEventStore>(), Arg.Any<object>());
    [Fact] void should_surface_the_resolution_failure() => _error.ShouldNotBeNull();

    class FailingInstances(IReactorSideEffectHandler first) : IInstancesOf<IReactorSideEffectHandler>
    {
        public IEnumerator<IReactorSideEffectHandler> GetEnumerator()
        {
            yield return first;
            throw new Exception("Cannot resolve another handler");
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
