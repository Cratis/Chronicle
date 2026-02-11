// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains;
using Cratis.Chronicle.Setup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Storage;
using Polly;
using Polly.Registry;
using Polly.Telemetry;

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
        // Based on https://devblogs.microsoft.com/dotnet/building-resilient-cloud-services-with-dotnet-8/#custom-resilience-pipeline-and-dynamic-reloads
        builder.Services.Configure<ResilientStorageOptions>(
            ResilientGrainStorage.ResiliencePipelineKey,
            builder.Configuration.GetSection(ConfigurationPath.Combine("Chronicle", "ResilientStorage")));
        builder.Services.AddResiliencePipeline(ResilientGrainStorage.ResiliencePipelineKey, static (builder, context) =>
        {
            context.EnableReloads<ResilientStorageOptions>();
            var options = context.GetOptions<ResilientStorageOptions>();
            builder.AddRetry(options.Retry);
            builder.AddTimeout(options.Timeout);

            var telemetryOptions = new TelemetryOptions(context.GetOptions<TelemetryOptions>())
            {
                SeverityProvider = ev =>
                {
                    if (options.ResilienceEventSeverities.TryGetValue(ev.Event.EventName, out var severity))
                    {
                        return severity;
                    }

                    return ev.Event.Severity;
                }
            };
            builder.ConfigureTelemetry(telemetryOptions);
        });

        builder.ConfigureServices(services =>
        {
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.Namespaces, CreateResilientStorageFor<Cratis.Chronicle.Grains.Namespaces.NamespacesStateStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.EventSequences, CreateResilientStorageFor<Cratis.Chronicle.Grains.EventSequences.EventSequencesStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.ObserverDefinitions, CreateResilientStorageFor<Cratis.Chronicle.Grains.Observation.ObserverDefinitionGrainStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.ObserverState, CreateResilientStorageFor<Cratis.Chronicle.Grains.Observation.ObserverStateGrainStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.FailedPartitions, CreateResilientStorageFor<Cratis.Chronicle.Grains.Observation.FailedPartitionGrainStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.Jobs, CreateResilientStorageFor<Cratis.Chronicle.Grains.Jobs.JobGrainStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.JobSteps, CreateResilientStorageFor<Cratis.Chronicle.Grains.Jobs.JobStepGrainStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.Recommendations, CreateResilientStorageFor<Cratis.Chronicle.Grains.Recommendations.RecommendationGrainStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.Projections, CreateResilientStorageFor<Cratis.Chronicle.Grains.Projections.ProjectionDefinitionStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.ProjectionsManager, CreateResilientStorageFor<Cratis.Chronicle.Grains.Projections.ProjectionsManagerStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.Webhooks, CreateResilientStorageFor<Cratis.Chronicle.Grains.Observation.Webhooks.WebhookDefinitionStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.WebhooksManager, CreateResilientStorageFor<Cratis.Chronicle.Grains.Observation.Webhooks.WebhooksStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.ProjectionFutures, CreateResilientStorageFor<Cratis.Chronicle.Grains.Projections.ProjectionFuturesStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.Reactors, CreateResilientStorageFor<Cratis.Chronicle.Grains.Observation.Reactors.ReactorDefinitionStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.Reducers, CreateResilientStorageFor<Cratis.Chronicle.Grains.Observation.Reducers.Clients.ReducerDefinitionStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.ReducersManager, CreateResilientStorageFor<Cratis.Chronicle.Grains.Observation.Reducers.Clients.ReducersManagerStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.Constraints, CreateResilientStorageFor<Cratis.Chronicle.Grains.Events.Constraints.ConstraintsStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.ReadModels, CreateResilientStorageFor<Cratis.Chronicle.Grains.ReadModels.ReadModelDefinitionStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.ReadModelsManager, CreateResilientStorageFor<Cratis.Chronicle.Grains.ReadModels.ReadModelsManagerStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.ReadModelReplayManager, CreateResilientStorageFor<Cratis.Chronicle.Grains.ReadModels.ReadModelReplayManagerStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.EventSeeding, CreateResilientStorageFor<Cratis.Chronicle.Grains.Seeding.EventSeedingGrainStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.DataProtectionKeys, CreateResilientStorageFor<Cratis.Chronicle.Grains.Security.DataProtectionKeysStorageProvider>);
        });

        return builder;
    }

#pragma warning disable CA1859
    static IGrainStorage CreateResilientStorageFor<TStorage>(IServiceProvider serviceProvider, object? context)
#pragma warning restore CA1859
        where TStorage : class, IGrainStorage
        => new ResilientGrainStorage(serviceProvider.GetRequiredService<TStorage>(), serviceProvider.GetRequiredService<ResiliencePipelineProvider<string>>());
}
