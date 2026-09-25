// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking;

public class handler_returning_an_unclaimed_task_result : Specification
{
    ReactorInvocationResult _result;
    ReactorInvoker _invoker;
    EventContext _eventContext;

    void Establish()
    {
        var eventTypes = new EventTypesForSpecifications([typeof(MyEvent)]);
        var reactor = new UnclaimedReactor();
        _invoker = new ReactorInvoker(
            eventTypes,
            Substitute.For<IReactorMiddlewares>(),
            typeof(UnclaimedReactor),
            new ActivatedArtifact(reactor, typeof(UnclaimedReactor), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>(),
            Substitute.For<IReactorSideEffectHandlers>(),
            Substitute.For<IEventStore>());
        _eventContext = EventContext.EmptyWithEventSourceId(EventSourceId.New());
    }

    async Task Because() => _result = await _invoker.Invoke(new MyEvent(), _eventContext);

    [Fact] void should_fail_the_invocation() => _result.IsFailed.ShouldBeTrue();
    [Fact] void should_name_the_unhandled_return_type() => _result.GetFailureDetails().Messages.Any(message => message.Contains(nameof(Unclaimed))).ShouldBeTrue();

    class UnclaimedReactor : IReactor
    {
        public Task<Unclaimed> Handle(MyEvent @event) => Task.FromResult(new Unclaimed());
    }

    record Unclaimed;
}
