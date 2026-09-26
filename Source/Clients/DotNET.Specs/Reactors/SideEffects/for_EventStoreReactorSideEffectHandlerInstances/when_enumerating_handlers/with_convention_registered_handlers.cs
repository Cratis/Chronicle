// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventStoreReactorSideEffectHandlerInstances.when_enumerating_handlers;

public class with_convention_registered_handlers : Specification
{
    ServiceProvider _provider;
    Type[] _discoveredTypes;
    Type[] _resolvedTypes;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddTypeDiscovery();
        services.AddCratisChronicleClient();
        services.AddSelfBindings();
        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        _discoveredTypes = TypeUniverse.For(_provider).FindMultiple<IReactorSideEffectHandler>().ToArray();
    }

    void Because() => _resolvedTypes = new EventStoreReactorSideEffectHandlerInstances(_provider)
        .Select(handler => handler.GetType()).ToArray();

    void Destroy() => _provider.Dispose();

    [Fact] void should_include_every_discovered_handler() => _resolvedTypes.ShouldContainOnly(_discoveredTypes);
    [Fact] void should_include_the_non_registry_handler() => _resolvedTypes.ShouldContain(typeof(EventForEventSourceIdResultHandler));
    [Fact] void should_include_each_handler_only_once() => _resolvedTypes.Distinct().Count().ShouldEqual(_resolvedTypes.Length);
}
