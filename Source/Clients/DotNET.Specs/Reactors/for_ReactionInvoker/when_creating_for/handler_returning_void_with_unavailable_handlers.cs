// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_creating_for;

public class handler_returning_void_with_unavailable_handlers : Specification
{
    Exception _error;
    IReactorSideEffectHandlers _handlers;

    void Establish()
    {
        _handlers = Substitute.For<IReactorSideEffectHandlers>();
        _handlers.CanHandleReturnType(Arg.Any<Type>()).Returns(_ => throw new Exception("Handlers must not be consulted"));
    }

    void Because() => _error = Catch.Exception(() => _ = new ReactorInvoker(
        new EventTypesForSpecifications([typeof(MyEvent)]),
        Substitute.For<IReactorMiddlewares>(),
        typeof(VoidReactor),
        new ActivatedArtifact(new VoidReactor(), typeof(VoidReactor), Substitute.For<ILogger<ActivatedArtifact>>()),
        Substitute.For<ILogger<ReactorInvoker>>(),
        _handlers));

    [Fact] void should_register_without_consulting_handlers() => _error.ShouldBeNull();

    class VoidReactor : IReactor
    {
        public void Handle(MyEvent @event) { }
    }
}
