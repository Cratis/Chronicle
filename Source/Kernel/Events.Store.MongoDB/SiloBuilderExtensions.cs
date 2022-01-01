// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Store;
using Cratis.Events.Store.Observation;
using Cratis.Events.Store.MongoDB;
using Cratis.Events.Store.MongoDB.Observation;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.Hosting
{
    /// <summary>
    /// Extension methods for <see cref="ISiloBuilder"/> for configuring event log stream.
    /// </summary>
    public static class SiloBuilderExtensions
    {
        /// <summary>
        /// Add event log stream support.
        /// </summary>
        /// <param name="builder"><see cref="ISiloBuilder"/> to add for.</param>
        /// <returns><see cref="ISiloBuilder"/> for builder continuation.</returns>
        public static ISiloBuilder AddEventLogStream(this ISiloBuilder builder)
        {
            builder.AddMemoryGrainStorage("PubSubStore");
            builder.ConfigureServices(services =>
            {
                //services.AddSingletonNamedService<IGrainStorage>("PubSubStore", (serviceProvider, _) => serviceProvider.GetService<EventLogPubSubStore>()!);
                services.AddSingletonNamedService<IGrainStorage>(EventLogState.StorageProvider, (serviceProvider, ___) => serviceProvider.GetService<EventLogStorageProvider>()!);
                services.AddSingletonNamedService<IGrainStorage>(ObserverState.StorageProvider, (serviceProvider, ___) => serviceProvider.GetService<ObserverStorageProvider>()!);
                services.AddSingletonNamedService<IGrainStorage>(PartitionedObserverState.StorageProvider, (serviceProvider, ___) => serviceProvider.GetService<PartitionedObserverStorageProvider>()!);
            });

            builder.AddPersistentStreams(
                "event-log",
                EventLogQueueAdapterFactory.Create,
                _ =>
                {
                    _.Configure<HashRingStreamQueueMapperOptions>(ob => ob.Configure(options => options.TotalQueueCount = 8));
                    _.ConfigureStreamPubSub();
                    // _.UseDynamicClusterConfigDeploymentBalancer();
                });
            return builder;
        }
    }
}
