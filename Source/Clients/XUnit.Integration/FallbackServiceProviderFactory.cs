// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// An <see cref="IServiceProviderFactory{TContainerBuilder}"/> that builds the standard
/// DI container and wraps the resulting <see cref="IServiceProvider"/> with a
/// <see cref="FallbackServiceProvider"/> so that types added to
/// <see cref="MutableServiceRegistry"/> after the container was built can still be resolved.
/// </summary>
internal sealed class FallbackServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
{
    /// <inheritdoc/>
    public IServiceCollection CreateBuilder(IServiceCollection services) => services;

    /// <inheritdoc/>
    public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
    {
        var options = new ServiceProviderOptions { ValidateOnBuild = false };
        var innerProvider = containerBuilder.BuildServiceProvider(options);

        var registry = innerProvider.GetService<MutableServiceRegistry>();
        if (registry is null)
        {
            File.AppendAllText("/tmp/chronicle-diag.log", "[DIAG-FACTORY] No MutableServiceRegistry found, returning raw provider\n");
            return innerProvider;
        }

        File.AppendAllText("/tmp/chronicle-diag.log", "[DIAG-FACTORY] Wrapping provider with FallbackServiceProvider\n");
        return new FallbackServiceProvider(innerProvider, registry);
    }
}
