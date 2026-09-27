// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventStoreReactorSideEffectHandlerInstances.when_enumerating_handlers;

public class with_a_captive_dependency : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() =>
    {
        var services = new ServiceCollection();
        services.AddTypeDiscovery();
        services.AddCratisChronicleClient();
        services.AddSelfBindings();
        services.AddScoped<ScopedDependency>();
        services.Replace(ServiceDescriptor.Singleton<EventForEventSourceIdResultHandler>(provider =>
        {
            _ = provider.GetRequiredService<ScopedDependency>();
            return new EventForEventSourceIdResultHandler();
        }));
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        _ = new EventStoreReactorSideEffectHandlerInstances(provider).ToArray();
    });

    [Fact] void should_reject_the_captive_dependency_on_the_discovered_handler() => _error.ShouldBeOfExactType<InvalidOperationException>();

    class ScopedDependency;
}
