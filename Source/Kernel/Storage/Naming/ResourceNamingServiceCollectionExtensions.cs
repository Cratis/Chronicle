// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// Extension methods for registering the resource name services.
/// </summary>
public static class ResourceNamingServiceCollectionExtensions
{
    /// <summary>
    /// Add the <see cref="IResourceNameGate"/>, <see cref="IResourceNameSnapshot"/> and <see cref="ResourceNameExemptions"/>, leaving existing registrations alone.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddResourceNameGate(this IServiceCollection services)
    {
        services.TryAddSingleton<ResourceNameSnapshot>();
        services.TryAddSingleton<IResourceNameSnapshot>(sp => sp.GetRequiredService<ResourceNameSnapshot>());
        services.TryAddSingleton(_ => ResourceNameExemptions.None);
        services.TryAddSingleton<IResourceNameGate, ResourceNameGate>();
        return services;
    }

    /// <summary>
    /// Add the <see cref="ResourceNameAudit"/>, and make the names unavailable until it has published its outcome.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for continuation.</returns>
    /// <remarks>
    /// Call this after the backend has added its services. It replaces the snapshot of <see cref="AddResourceNameGate"/>,
    /// which is ready by default, so hosts that do not run the audit keep working.
    /// </remarks>
    public static IServiceCollection AddResourceNameAudit(this IServiceCollection services)
    {
        services.RemoveAll<ResourceNameSnapshot>();
        services.RemoveAll<IResourceNameSnapshot>();
        services.AddSingleton(_ => ResourceNameSnapshot.NotReady());
        services.AddSingleton<IResourceNameSnapshot>(sp => sp.GetRequiredService<ResourceNameSnapshot>());
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ResourceNameAudit>();
        return services;
    }
}
