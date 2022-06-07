// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Boot;
using Aksio.Cratis.Configuration;
using Aksio.Cratis.Events.Projections.Grains;
using Aksio.Cratis.Events.Schemas;
using Aksio.Cratis.Events.Store.Grains.Inboxes;
using Aksio.Cratis.Events.Store.Inboxes;
using Aksio.Cratis.Execution;
using Orleans;

namespace Aksio.Cratis.Server;

/// <summary>
/// Represents a <see cref="IPerformBootProcedure"/> for the event store.
/// </summary>
public class BootProcedure : IPerformBootProcedure
{
    readonly IServiceProvider _serviceProvider;
    readonly IExecutionContextManager _executionContextManager;
    readonly IGrainFactory _grainFactory;
    readonly KernelConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="BootProcedure"/> class.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting services.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
    /// <param name="configuration">The <see cref="KernelConfiguration"/>.</param>
    public BootProcedure(
        IServiceProvider serviceProvider,
        IExecutionContextManager executionContextManager,
        IGrainFactory grainFactory,
        KernelConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _executionContextManager = executionContextManager;
        _grainFactory = grainFactory;
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public void Perform()
    {
        foreach (var (microserviceId, microservice) in _configuration.Microservices)
        {
            _executionContextManager.Establish(microserviceId);
            var schemaStore = _serviceProvider.GetService<ISchemaStore>()!;
            schemaStore.Populate().Wait();

            var projections = _grainFactory.GetGrain<IProjections>((MicroserviceId)microserviceId);
            projections.Rehydrate().Wait();

            foreach (var outbox in microservice.Inbox.FromOutboxes)
            {
                foreach (var (tenantId, _) in _configuration.Tenants)
                {
                    var key = new InboxKey(tenantId, outbox.Microservice);
                    var inbox = _grainFactory.GetGrain<IInbox>((MicroserviceId)microserviceId, key);
                    inbox.Start().Wait();
                }
            }
        }
    }
}
