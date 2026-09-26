// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorSideEffectHandlers;

public class when_a_discovered_handler_is_not_registered : Specification
{
    Type[] _resolvedTypes;
    Exception _error;

    void Because() => _error = Catch.Exception(() =>
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        _resolvedTypes = new EventStoreReactorSideEffectHandlerInstances(provider)
            .Select(handler => handler.GetType()).ToArray();
    });

    [Fact] void should_not_fail_resolution() => _error.ShouldBeNull();
    [Fact] void should_keep_the_event_handler() => _resolvedTypes.ShouldContain(typeof(EventResultHandler));
    [Fact] void should_skip_the_unregistered_handler() => _resolvedTypes.ShouldNotContain(typeof(EventForEventSourceIdResultHandler));
}
