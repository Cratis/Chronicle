// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_creating_for;

public class handler_returning_an_unclaimed_type : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _ = new ReactorInvoker(
        new EventTypesForSpecifications([typeof(MyEvent)]),
        Substitute.For<IReactorMiddlewares>(),
        typeof(UnclaimedReactor),
        new ActivatedArtifact(new UnclaimedReactor(), typeof(UnclaimedReactor), Substitute.For<ILogger<ActivatedArtifact>>()),
        Substitute.For<ILogger<ReactorInvoker>>(),
        Substitute.For<IReactorSideEffectHandlers>()));

    [Fact] void should_reject_the_return_type() => _error.ShouldBeOfExactType<InvalidReactorHandlerReturnType>();

    class UnclaimedReactor : IReactor
    {
        public Unclaimed Handle(MyEvent @event) => new();
    }

    record Unclaimed;
}
