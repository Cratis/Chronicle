// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Clients;
using Aksio.Cratis.Kernel.Grains.Configuration.Tenants;
using Aksio.Cratis.Kernel.Grains.EventSequences;
using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Kernel.MongoDB;
using Aksio.Cratis.Kernel.MongoDB.Clients;
using Aksio.Cratis.Kernel.MongoDB.Observation;
using Aksio.Cratis.Kernel.MongoDB.Reminders;
using Aksio.Cratis.Kernel.MongoDB.Tenants;
using Aksio.Cratis.Kernel.Observation;
using Aksio.Cratis.Kernel.Persistence.Observation;
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
            services.AddSingletonNamedService<IGrainStorage>(EventSequenceState.StorageProvider, (serviceProvider, _) => serviceProvider.GetRequiredService<EventSequencesStorageProvider>());
            services.AddSingletonNamedService<IGrainStorage>(ObserverState.StorageProvider, (serviceProvider, _) => serviceProvider.GetRequiredService<ObserverGrainStorageProvider>());
            services.AddSingletonNamedService<IGrainStorage>(RecoverFailedPartitionState.StorageProvider, (serviceProvider, _) => serviceProvider.GetRequiredService<RecoverFailedPartitionStorageProvider>());
            services.AddSingletonNamedService<IGrainStorage>(TenantConfigurationState.StorageProvider, (serviceProvider, _) => serviceProvider.GetRequiredService<TenantConfigurationStorageProvider>());
            services.AddSingletonNamedService<IGrainStorage>(ConnectedClientsState.StorageProvider, (serviceProvider, _) => serviceProvider.GetRequiredService<ConnectedClientsStorageProvider>());
        });
        builder.ConfigureServices(services => services.AddSingleton<IReminderTable, MongoDBReminderTable>());
        return builder;
    }
}
