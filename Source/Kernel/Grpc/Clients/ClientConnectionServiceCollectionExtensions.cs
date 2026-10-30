// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Chronicle.Services.Clients;

/// <summary>
/// Defines registration of the connections open on the local silo.
/// </summary>
internal static class ClientConnectionServiceCollectionExtensions
{
    /// <summary>
    /// Registers one connection store shared by the gRPC service and the client registry.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <returns>The <see cref="IServiceCollection"/> for continuation.</returns>
    internal static IServiceCollection AddLocalClientConnections(this IServiceCollection services)
    {
        services.RemoveAll<OpenConnections>();
        services.AddSingleton<OpenConnections>();
        services.RemoveAll<ILocalClientConnections>();
        services.AddSingleton<ILocalClientConnections>(sp => sp.GetRequiredService<OpenConnections>());
        return services;
    }
}
