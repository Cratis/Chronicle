// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventStoreReactorSideEffectHandlerInstances;

public class when_validating_convention_registered_handlers : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() =>
    {
        var services = new ServiceCollection();
        services.AddTypeDiscovery();
        services.AddCratisChronicleClient();
        services.AddSelfBindings();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        _ = new EventStoreReactorSideEffectHandlerInstances(provider).ToArray();
    });

    [Fact] void should_not_capture_a_scoped_service_in_any_discovered_handler() => _error.ShouldBeNull();
}
