// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Client;

/// <summary>
/// Defines the builder for building a <see cref="ISingleTenantClient"/>.
/// </summary>
public class SingleTenantClientBuilder : ClientBuilder<ISingleTenantClientBuilder, ISingleTenantClient>, ISingleTenantClientBuilder
{
    /// <inheritdoc/>
    protected override ISingleTenantClient BuildActual(IServiceCollection services, IClientArtifactsProvider clientArtifacts, ILoggerFactory loggerFactory)
    {
        var serviceProvider = services.BuildServiceProvider();
        var client = new SingleTenantClient(
            serviceProvider,
            serviceProvider.GetRequiredService<IEventTypes>(),
            serviceProvider.GetRequiredService<IEventSerializer>(),
            serviceProvider.GetRequiredService<IExecutionContextManager>());

        services
                .AddSingleton(sp => client.EventStore.Sequences.EventLog)
                .AddSingleton(sp => client.EventStore.Sequences.Outbox);

        return client;
    }
}
