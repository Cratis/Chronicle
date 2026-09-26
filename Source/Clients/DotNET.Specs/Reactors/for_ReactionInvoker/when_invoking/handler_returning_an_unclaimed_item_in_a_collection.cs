// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking;

public class handler_returning_an_unclaimed_item_in_a_collection : Specification
{
    ReactorInvocationResult _result;
    Exception _error;
    ReactorInvoker _invoker;

    void Establish()
    {
        _invoker = new ReactorInvoker(
            new EventTypesForSpecifications([typeof(MyEvent)]),
            Substitute.For<IReactorMiddlewares>(),
            typeof(UnclaimedCollectionReactor),
            new ActivatedArtifact(new UnclaimedCollectionReactor(), typeof(UnclaimedCollectionReactor), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>(),
            Substitute.For<IReactorSideEffectHandlers>(),
            Substitute.For<IEventStore>());
    }

    async Task Because()
    {
        _result = await _invoker.Invoke(new MyEvent(), EventContext.EmptyWithEventSourceId(EventSourceId.New()));
        _result.ExceptionResult.TryGetException(out _error);
    }

    [Fact] void should_fail_the_invocation() => _result.IsFailed.ShouldBeTrue();
    [Fact] void should_report_an_unhandled_return_value() => _error.ShouldBeOfExactType<UnhandledReactorReturnValue>();

    class UnclaimedCollectionReactor : IReactor
    {
        public IEnumerable<object> Handle(MyEvent @event) => [new object()];
    }
}
