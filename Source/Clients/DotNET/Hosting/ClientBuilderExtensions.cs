// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Events.Store.EventLogs;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Orleans;

/// <summary>
/// Extension methods for <see cref="IClientBuilder"/>.
/// </summary>
public static class ClientBuilderExtensions
{
    /// <summary>
    /// Add the event log stream.
    /// </summary>
    /// <param name="builder"><see cref="IClientBuilder"/> to add to.</param>
    /// <returns><see cref="IClientBuilder"/> for continuation.</returns>
    public static IClientBuilder AddEventSequenceStream(this IClientBuilder builder)
    {
        builder.AddPersistentStreams(
            WellKnownProviders.EventSequenceStreamProvider,
            EventLogQueueAdapterFactory.Create,
            _ => { });
        return builder;
    }

    /// <summary>
    /// Sets up a outgoing call filter that adds the connection identifier from the connection context to every call.
    /// </summary>
    /// <param name="builder"><see cref="IClientBuilder"/> to add to.</param>
    /// <returns><see cref="IClientBuilder"/> for continuation.</returns>
    public static IClientBuilder UseConnectionIdFromConnectionContextForOutgoingCalls(this IClientBuilder builder)
    {
        return builder
                .AddOutgoingGrainCallFilter<ConnectionIdOutgoingCallFilter>()
                .Configure<ClientConnectionOptions>(options =>
                {
                    options.ConfigureConnection(connectionBuilder =>
                    {
                        connectionBuilder.Use(next =>
                        {
                            return async context =>
                            {
                                await next(context);

                                // TODO: Kernel disconnected, we can start retrying
                            };
                        });
                    });
                });
    }
}
