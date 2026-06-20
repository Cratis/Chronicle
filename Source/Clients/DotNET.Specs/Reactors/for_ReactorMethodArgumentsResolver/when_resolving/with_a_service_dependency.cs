// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ReactorMethodArgumentsResolver.when_resolving;

public class with_a_service_dependency : given.a_resolver
{
    ISomeService _service;
    object?[] _arguments;

    void Establish()
    {
        _service = Substitute.For<ISomeService>();
        _serviceProvider.GetService(typeof(ISomeService)).Returns(_service);
    }

    async Task Because() => _arguments = await _resolver.Resolve(
        MethodNamed(nameof(ReactorWithDependencies.WithService)),
        new ReactorWithDependencies(),
        _event,
        _eventContext,
        _eventStore,
        _serviceProvider);

    [Fact] void should_pass_the_event_as_the_first_argument() => _arguments[0].ShouldEqual(_event);
    [Fact] void should_resolve_the_service_from_the_service_provider() => _arguments[1].ShouldEqual(_service);
}
