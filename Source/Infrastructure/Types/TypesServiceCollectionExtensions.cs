// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Types;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for <see cref="IServiceCollection"/> for setting up type discovery.
/// </summary>
public static class TypesServiceCollectionExtensions
{
    /// <summary>
    /// Adds type discovery to the service collection.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="assemblyProviders">Optional collection of <see cref="ICanProvideAssembliesForDiscovery"/>. Will default to <see cref="ProjectReferencedAssemblies"/> and <see cref="PackageReferencedAssemblies"/>.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddTypeDiscovery(this IServiceCollection services, IEnumerable<ICanProvideAssembliesForDiscovery>? assemblyProviders)
    {
        var types = assemblyProviders is null ? new Types() : new Types(assemblyProviders);
        services.AddSingleton<ITypes>(types);
        services
            .AddTransient(typeof(IInstancesOf<>), typeof(InstancesOf<>))
            .AddTransient(typeof(IImplementationsOf<>), typeof(ImplementationsOf<>));
        return services;
    }
}
