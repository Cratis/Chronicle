// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ReactorMethodArgumentsResolver.when_resolving;

public class with_event_and_context : given.a_resolver
{
    object?[] _arguments;

    async Task Because() => _arguments = await _resolver.Resolve(
        MethodNamed(nameof(ReactorWithDependencies.EventAndContext)),
        new ReactorWithDependencies(),
        _event,
        _eventContext,
        _eventStore,
        _serviceProvider);

    [Fact] void should_pass_the_event_as_the_first_argument() => _arguments[0].ShouldEqual(_event);
    [Fact] void should_pass_the_event_context_as_the_second_argument() => _arguments[1].ShouldEqual(_eventContext);
}
