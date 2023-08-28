// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Boot;
using Aksio.Cratis.EventSequences.Inboxes;
using Aksio.Cratis.Identities;
using Aksio.Cratis.Kernel.Configuration;
using Aksio.Cratis.Kernel.Grains.EventSequences.Inbox;
using Aksio.Cratis.Kernel.Grains.EventSequences.Streaming;
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
    readonly ILogger<BootProcedure> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BootProcedure"/> class.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting services.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
    /// <param name="configuration">The <see cref="KernelConfiguration"/>.</param>
    /// <param name="logger">Logger for logging.</param>
    public BootProcedure(
        IServiceProvider serviceProvider,
        IExecutionContextManager executionContextManager,
        IGrainFactory grainFactory,
        KernelConfiguration configuration,
        ILogger<BootProcedure> logger)
    {
        _serviceProvider = serviceProvider;
        _executionContextManager = executionContextManager;
        _grainFactory = grainFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc/>
    public void Perform()
    {
        _ = Task.Run(() =>
        {
            _logger.PrimingEventSequenceCaches();
            var eventSequenceCaches = _serviceProvider.GetRequiredService<IEventSequenceCaches>()!;
            eventSequenceCaches.PrimeAll().Wait();

            foreach (var (microserviceId, microservice) in _configuration.Microservices)
            {
                _executionContextManager.Establish(microserviceId);

                this._logger.PopulateSchemaStore();
                var schemaStore = _serviceProvider.GetRequiredService<Schemas.ISchemaStore>()!;
                schemaStore.Populate().Wait();

                this._logger.PopulateIdentityStore();
                var identityStore = _serviceProvider.GetRequiredService<IIdentityStore>()!;
                identityStore.Populate().Wait();

                _logger.RehydrateProjections();
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
