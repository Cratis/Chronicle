// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Grains.Observation;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;

namespace Orleans.Hosting
{
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
                        var connectedClients = connectionBuilder.ApplicationServices.GetService<IConnectedClients>()!;
                        return async context =>
                        {
                            await connectedClients.OnClientConnected(context);
                            await next(context);
                            await connectedClients.OnClientDisconnected(context);
                        };
                    });
                });
            });

            return builder;
        }
    }
}
