// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Grains.Connections;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;

namespace Orleans.Hosting;

/// <summary>
/// Extension methods for extending the <see cref="ISiloBuilder"/>.
/// </summary>
public static class SiloBuilderExtensions
{
    /// <summary>
    /// Add a tracker of connected clients.
    /// </summary>
    /// <param name="builder"><see cref="ISiloBuilder"/> to add to.</param>
    /// <returns><see cref="ISiloBuilder"/> for continuation.</returns>
    public static ISiloBuilder AddConnectedClientsTracking(this ISiloBuilder builder)
    {
        builder.Configure<SiloConnectionOptions>(options =>
        {
            options.ConfigureGatewayInboundConnection(connectionBuilder =>
            {
                connectionBuilder.Use(next =>
                {
                    var connectedClients = connectionBuilder.ApplicationServices.GetService<IGrainFactory>()!.GetGrain<IConnectedClients>(Guid.Empty);
                    return async context =>
                    {
                        Console.WriteLine(context.ConnectionId);
                        await connectedClients.OnClientConnected(context.ConnectionId);
                        await next(context);
                        await connectedClients.OnClientDisconnected(context.ConnectionId);
                    };
                });
            });
        });

        return builder;
    }
}
