// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_creating_for;

public class handler_returning_a_collection_of_claimed_types : Specification
{
    Exception _error;
    IReactorSideEffectHandlers _handlers;
    ReactorInvoker _invoker;

    void Establish()
    {
        _handlers = Substitute.For<IReactorSideEffectHandlers>();
        _handlers.CanHandleReturnType(typeof(Claimed)).Returns(true);
    }

    void Because() => _error = Catch.Exception(() => _invoker = new ReactorInvoker(
        new EventTypesForSpecifications([typeof(MyEvent)]),
        Substitute.For<IReactorMiddlewares>(),
        typeof(ClaimedCollectionReactor),
        new ActivatedArtifact(new ClaimedCollectionReactor(), typeof(ClaimedCollectionReactor), Substitute.For<ILogger<ActivatedArtifact>>()),
        Substitute.For<ILogger<ReactorInvoker>>(),
        _handlers));

    [Fact] void should_accept_the_collection() => _error.ShouldBeNull();

    class ClaimedCollectionReactor : IReactor
    {
        public IEnumerable<Claimed> Handle(MyEvent @event) => [new Claimed()];
    }

    record Claimed;
}
