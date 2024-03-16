// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Boot;
using Cratis.Events;
using Cratis.Kernel.Configuration;
using Cratis.Kernel.Contracts.Events;
using Cratis.Kernel.Grains.EventSequences;
using Cratis.Kernel.Grains.EventSequences.Streaming;
using Cratis.Kernel.Grains.Jobs;
using Cratis.Kernel.Grains.Projections;
using Cratis.Kernel.Storage;
using Cratis.Observation;
using Cratis.Schemas;
using NJsonSchema;
using IEventTypes = Cratis.Events.IEventTypes;
using IObservers = Cratis.Kernel.Grains.Observation.IObservers;

namespace Cratis.Kernel.Server;

/// <summary>
/// Represents a <see cref="IPerformBootProcedure"/> for the event store.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BootProcedure"/> class.
/// </remarks>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting services.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
/// <param name="configuration">The <see cref="KernelConfiguration"/>.</param>
/// <param name="eventTypes"><see cref="IEventTypes"/> for registering.</param>
/// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
/// <param name="logger">Logger for logging.</param>
public class BootProcedure(
    IServiceProvider serviceProvider,
    IGrainFactory grainFactory,
    KernelConfiguration configuration,
    IEventTypes eventTypes,
    IJsonSchemaGenerator schemaGenerator,
    ILogger<BootProcedure> logger) : IPerformBootProcedure
{
    /// <inheritdoc/>
    public void Perform()
    {
        Task.Run(async () =>
        {
            var eventTypeRegistrations = eventTypes.AllClrTypes.Select(_ =>
            {
                var type = _;
                return new EventTypeRegistration
                {
                    Type = eventTypes.GetEventTypeFor(type)!.ToContract(),
                    FriendlyName = type.Name,
                    Schema = schemaGenerator.Generate(type).ToJson()
                };
            });

            var storage = serviceProvider.GetRequiredService<IStorage>();

            foreach (var (microserviceId, microservice) in configuration.Microservices)
            {
                logger.PopulateSchemaStore();
                var eventStoreStorage = storage.GetEventStore(microserviceId);
                await eventStoreStorage.EventTypes.Populate();

                foreach (var eventTypeRegistration in eventTypeRegistrations)
                {
                    var schema = await JsonSchema.FromJsonAsync(eventTypeRegistration.Schema);
                    await eventStoreStorage.EventTypes.Register(
                        eventTypeRegistration.Type.ToKernel(),
                        eventTypeRegistration.FriendlyName,
                        schema);
                }

                logger.PopulateIdentityStore();
                await eventStoreStorage.Identities.Populate();

                foreach (var (tenantId, _) in configuration.Tenants)
                {
                    logger.RehydratingEventSequences(microserviceId, tenantId);
                    var stopwatch = Stopwatch.StartNew();
                    await grainFactory.GetGrain<IEventSequences>(0, new EventSequencesKey(microserviceId, tenantId)).Rehydrate();
                    stopwatch.Stop();
                    logger.RehydratedEventSequences(microserviceId, tenantId, stopwatch.Elapsed);

                    logger.RehydrateJobs(microserviceId, tenantId);
                    await grainFactory.GetGrain<IJobsManager>(0, new JobsManagerKey(microserviceId, tenantId)).Rehydrate();

                    logger.RehydrateObservers(microserviceId, tenantId);
                    await grainFactory.GetGrain<IObservers>(0, new ObserversKey(microserviceId, tenantId)).Rehydrate();
                }
            }

            logger.RehydrateProjections();
            var projections = grainFactory.GetGrain<IProjections>(0);
            await projections.Rehydrate();

            logger.PrimingEventSequenceCaches();
            var eventSequenceCaches = serviceProvider.GetRequiredService<IEventSequenceCaches>()!;
            await eventSequenceCaches.PrimeAll();
        }).Wait();
    }
}
