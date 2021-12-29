// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Copyright (c) Cratis. All rights reserved.

using Cratis.Events.Store.Grains.Observation;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;

namespace Orleans.Hosting
{
    /// <summary>
    /// Extension methods for extending the <see cref="ISiloBuilder"/>.
    /// </summary>
    public static class SiloBuilderExtensions
    {
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
                            await connectedClients.ClientConnected(context);
                            await next(context);
                            await connectedClients.ClientDisconnected(context);
                        };
                    });
                });
            });

            return builder;
        }
    }
}
