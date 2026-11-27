// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Server.for_KernelHostComposition.when_registering_namespace_removal;

public class and_resolving_both_contracts : Specification
{
    [Fact]
    public async Task should_share_the_namespace_remover_across_contracts_and_scopes()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Host.UseOrleans(silo => silo.AddChronicleToSilo());
        builder.Services.AddBindingsByConvention().AddSelfBindings();

        var services = new ServiceCollection().AddLogging();
        services.AddSingleton(Substitute.For<IGrainFactory>());
        services.AddSingleton(Substitute.For<IStorage>());
        services.AddSingleton(Substitute.For<IReminderTable>());
        services.AddSingleton(Substitute.For<INamespaceCasing>());
        foreach (var descriptor in builder.Services.Where(_ =>
                     _.ServiceType == typeof(NamespaceRemover) ||
                     _.ServiceType == typeof(INamespaceRemover) ||
                     _.ServiceType == typeof(INamespaceRemovalSteps)))
        {
            services.Add(descriptor);
        }

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        await using var scope = provider.CreateAsyncScope();
        ReferenceEquals(provider.GetRequiredService<INamespaceRemover>(), scope.ServiceProvider.GetRequiredService<INamespaceRemovalSteps>()).ShouldBeTrue();
    }
}
