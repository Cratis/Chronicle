// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Boot;
using Aksio.Cratis.EventSequences.Inboxes;
using Aksio.Cratis.Kernel.Configuration;
using Aksio.Cratis.Kernel.Grains.EventSequences.Inbox;
using Aksio.Cratis.Kernel.Grains.Projections;

namespace Aksio.Cratis.Kernel.Server;

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
        _ = Task.Run(() =>
        {
            foreach (var (microserviceId, microservice) in _configuration.Microservices)
            {
                _executionContextManager.Establish(microserviceId);
                var schemaStore = _serviceProvider.GetRequiredService<Schemas.ISchemaStore>()!;
                schemaStore.Populate().Wait();

                var causedByStore = _serviceProvider.GetRequiredService<Auditing.ICausedByStore>()!;
                causedByStore.Populate().Wait();

                var projections = _grainFactory.GetGrain<IProjections>(0);
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
        });
    }
}
