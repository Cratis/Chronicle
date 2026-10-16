// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Extension methods for registering the <see cref="IStorageConnectionResolution"/>.
/// </summary>
public static class StorageConnectionResolutionServiceCollectionExtensions
{
    /// <summary>
    /// Register the connection resolution of the storage, replacing any registered before.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="storage">The kind of storage, one of <see cref="WellKnownConnectionStorage"/>.</param>
    /// <param name="options">The configured <see cref="ConnectionResolutionOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddStorageConnectionResolution(this IServiceCollection services, string storage, ConnectionResolutionOptions options)
    {
        services.AddConnectionResolution();
        services.Replace(ServiceDescriptor.Singleton<IStorageConnectionResolution>(sp =>
            new StorageConnectionResolution(storage, options, sp.GetRequiredService<IConnectionResolverFactory>())));
        return services;
    }
}
