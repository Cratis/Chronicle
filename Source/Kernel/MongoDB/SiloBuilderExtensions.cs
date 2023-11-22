// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis;
using Aksio.Cratis.Kernel.MongoDB;
using Aksio.Cratis.Kernel.MongoDB.Reminders;
using Aksio.Cratis.Kernel.MongoDB.Tenants;
using Aksio.Cratis.Kernel.Persistence.Jobs;
using Aksio.Cratis.Kernel.Persistence.Observation;
using Aksio.Cratis.Kernel.Persistence.Recommendations;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.Hosting;

/// <summary>
/// Extension methods for <see cref="ISiloBuilder"/> for configuring event sequence stream.
/// </summary>
public static class SiloBuilderExtensions
{
    /// <summary>
    /// Add event sequence stream support.
    /// </summary>
    /// <param name="builder"><see cref="ISiloBuilder"/> to add for.</param>
    /// <returns><see cref="ISiloBuilder"/> for builder continuation.</returns>
    public static ISiloBuilder UseMongoDB(this ISiloBuilder builder)
    {
        // TODO: Store Grain state in Mongo
        builder.AddMemoryGrainStorage("PubSubStore");
        builder.ConfigureServices(services =>
        {
            services.AddSingletonNamedService<IGrainStorage>(WellKnownGrainStorageProviders.EventSequences, (serviceProvider, _) => serviceProvider.GetRequiredService<EventSequencesStorageProvider>());
            services.AddSingletonNamedService<IGrainStorage>(WellKnownGrainStorageProviders.Observers, (serviceProvider, _) => serviceProvider.GetRequiredService<ObserverGrainStorageProvider>());
            services.AddSingletonNamedService<IGrainStorage>(WellKnownGrainStorageProviders.FailedPartitions, (serviceProvider, _) => serviceProvider.GetRequiredService<FailedPartitionGrainStorageProvider>());
            services.AddSingletonNamedService<IGrainStorage>(WellKnownGrainStorageProviders.TenantConfiguration, (serviceProvider, _) => serviceProvider.GetRequiredService<TenantConfigurationStorageProvider>());
            services.AddSingletonNamedService<IGrainStorage>(WellKnownGrainStorageProviders.Jobs, (serviceProvider, _) => serviceProvider.GetRequiredService<JobGrainStorageProvider>());
            services.AddSingletonNamedService<IGrainStorage>(WellKnownGrainStorageProviders.JobSteps, (serviceProvider, _) => serviceProvider.GetRequiredService<JobStepGrainStorageProvider>());
            services.AddSingletonNamedService<IGrainStorage>(WellKnownGrainStorageProviders.Recommendations, (serviceProvider, _) => serviceProvider.GetRequiredService<RecommendationGrainStorageProvider>());
        });
        builder.ConfigureServices(services => services.AddSingleton<IReminderTable, MongoDBReminderTable>());
        return builder;
    }
}
