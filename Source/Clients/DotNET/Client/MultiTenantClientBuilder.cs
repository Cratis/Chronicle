// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Client;

/// <summary>
/// Represents an implementation of <see cref="IMultiTenantClientBuilder"/>.
/// </summary>
public class MultiTenantClientBuilder : ClientBuilder<IMultiTenantClientBuilder, IMultiTenantClient>, IMultiTenantClientBuilder
{
    /// <inheritdoc/>
    protected override IMultiTenantClient BuildActual(IServiceCollection services, IClientArtifactsProvider clientArtifacts, ILoggerFactory? loggerFactory)
    {
        var serviceProvider = services.BuildServiceProvider();
        var client = new MultiTenantClient(
            () => services.BuildServiceProvider(),
            serviceProvider.GetRequiredService<IEventTypes>(),
            serviceProvider.GetRequiredService<IEventSerializer>(),
            serviceProvider.GetRequiredService<IExecutionContextManager>());

        services
                .AddTransient(sp =>
                {
                    var tenantId = sp.GetRequiredService<IExecutionContextManager>().Current.TenantId;
                    return client.EventStore.Sequences.ForTenant(tenantId).EventLog;
                })
                .AddTransient(sp =>
                {
                    var tenantId = sp.GetRequiredService<IExecutionContextManager>().Current.TenantId;
                    return client.EventStore.Sequences.ForTenant(tenantId).Outbox;
                });

        return client;
    }
}
