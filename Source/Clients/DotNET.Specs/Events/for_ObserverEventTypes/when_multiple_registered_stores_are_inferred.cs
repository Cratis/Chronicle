// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Events.for_ObserverEventTypes;

public class when_multiple_registered_stores_are_inferred : Specification
{
    IEventTypes _registry;
    Exception _reactorError;
    Exception _reducerError;

    void Establish() => _registry = RegisteredEventTypesForSpecifications.Create(
        (typeof(when_resolving_registered_handlers.Foreign), new("foreign", 1)),
        (typeof(when_resolving_registered_handlers.Local), new("local", 1)));

    void Because()
    {
        _reactorError = Catch.Exception(() => ReactorTypeExtensions.GetEventSequenceId(typeof(when_resolving_registered_handlers.Reactor), _registry, "current"));
        _reducerError = Catch.Exception(() => ReducerTypeExtensions.GetEventSequenceId(typeof(when_resolving_registered_handlers.Reducer), _registry, "current", typeof(when_resolving_registered_handlers.Model)));
    }

    [Fact] void should_reject_multiple_reactor_source_stores() => _reactorError.ShouldBeOfExactType<MultipleEventStoresDefined>();
    [Fact] void should_reject_multiple_reducer_source_stores() => _reducerError.ShouldBeOfExactType<MultipleEventStoresDefined>();
}
