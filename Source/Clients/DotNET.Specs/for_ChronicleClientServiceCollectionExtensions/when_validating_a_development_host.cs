// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.for_ChronicleClientServiceCollectionExtensions;

public class when_validating_a_development_host : Specification
{
    ServiceProvider _provider;
    Exception _error;

    void Because()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Development });
        builder.AddCratisChronicle();
        _error = Catch.Exception(() => _provider = builder.Services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        }));
    }

    void Destroy() => _provider?.Dispose();

    [Fact] void should_validate_all_registered_services() => _error.ShouldBeNull();
    [Fact] void should_resolve_the_artifact_activator() => _provider.GetRequiredService<IClientArtifactsActivator>().ShouldBeOfExactType<ClientArtifactsActivator>();
    [Fact] void should_resolve_the_scoped_side_effect_dispatcher()
    {
        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IReactorSideEffectHandlers>().ShouldBeOfExactType<ReactorSideEffectHandlers>();
    }
}
