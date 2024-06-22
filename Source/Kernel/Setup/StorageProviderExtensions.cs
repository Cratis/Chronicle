// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Storage;

namespace Orleans.Hosting;

/// <summary>
/// Defines extensions for <see cref="ISiloBuilder"/> for configuring storage providers.
/// </summary>
public static class StorageProviderExtensions
{
    /// <summary>
    /// Add storage providers to the silo.
    /// </summary>
    /// <param name="builder">The <see cref="ISiloBuilder"/> to add to.</param>
    /// <returns><see cref="ISiloBuilder"/> for continuation.</returns>
    public static ISiloBuilder AddStorageProviders(this ISiloBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // TODO: From Server project we have copied the following, this should be consolidated into a new place
            services.AddKeyedSingleton<IGrainStorage>(WellKnownGrainStorageProviders.EventSequences, (serviceProvider, _) => serviceProvider.GetRequiredService<Cratis.Chronicle.Grains.EventSequences.EventSequencesStorageProvider>());
            services.AddKeyedSingleton<IGrainStorage>(WellKnownGrainStorageProviders.Observers, (serviceProvider, _) => serviceProvider.GetRequiredService<Cratis.Chronicle.Grains.Observation.ObserverGrainStorageProvider>());
            services.AddKeyedSingleton<IGrainStorage>(WellKnownGrainStorageProviders.FailedPartitions, (serviceProvider, _) => serviceProvider.GetRequiredService<Cratis.Chronicle.Storage.Observation.FailedPartitionGrainStorageProvider>());
            services.AddKeyedSingleton<IGrainStorage>(WellKnownGrainStorageProviders.Jobs, (serviceProvider, _) => serviceProvider.GetRequiredService<Cratis.Chronicle.Grains.Jobs.JobGrainStorageProvider>());
            services.AddKeyedSingleton<IGrainStorage>(WellKnownGrainStorageProviders.JobSteps, (serviceProvider, _) => serviceProvider.GetRequiredService<Cratis.Chronicle.Grains.Jobs.JobStepGrainStorageProvider>());
            services.AddKeyedSingleton<IGrainStorage>(WellKnownGrainStorageProviders.Recommendations, (serviceProvider, _) => serviceProvider.GetRequiredService<Cratis.Chronicle.Grains.Recommendations.RecommendationGrainStorageProvider>());
        });

        return builder;
    }
}
