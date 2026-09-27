// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorSideEffectHandlers;

public class when_a_handler_is_registered_only_by_interface : Specification
{
    ServiceProvider _provider;
    IReactorSideEffectHandler[] _handlers;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IReactorSideEffectHandler, when_a_handler_is_registered_by_interface_and_class.CustomHandler>();
        _provider = services.BuildServiceProvider();
    }

    void Because() => _handlers = new EventStoreReactorSideEffectHandlerInstances(_provider).ToArray();
    void Destroy() => _provider.Dispose();

    [Fact] void should_resolve_the_custom_handler() => _handlers.Count(handler => handler is when_a_handler_is_registered_by_interface_and_class.CustomHandler).ShouldEqual(1);
}
