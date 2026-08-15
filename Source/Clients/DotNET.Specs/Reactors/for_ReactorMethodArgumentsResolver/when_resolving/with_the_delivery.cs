// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ReactorMethodArgumentsResolver.when_resolving;

/// <summary>
/// The delivery identity is reachable the same way the event context is - by declaring a parameter for it - so a
/// reactor never has to reach for anything of the kernel's to write an idempotent handler.
/// </summary>
public class with_the_delivery : given.a_resolver
{
    ReactorWithDependencies _reactor;
    object?[] _arguments;

    void Establish() => _reactor = new ReactorWithDependencies();

    async Task Because() => _arguments = await _resolver.Resolve(
        MethodNamed(nameof(ReactorWithDependencies.WithDelivery)),
        _reactor,
        _event,
        _eventContext,
        _eventStore,
        _serviceProvider);

    [Fact] void should_pass_the_event_as_the_first_argument() => _arguments[0].ShouldEqual(_event);
    [Fact] void should_pass_the_delivery_as_the_second_argument() => _arguments[1].ShouldEqual(ReactorDelivery.For(_reactor, _eventContext));
    [Fact] void should_not_go_through_the_service_provider() => _serviceProvider.DidNotReceive().GetService(typeof(ReactorDelivery));
}
