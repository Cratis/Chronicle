// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
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
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.Namespaces, CreateResilientStorageFor<Cratis.Chronicle.Namespaces.NamespacesStateStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.EventSequences, CreateResilientStorageFor<Cratis.Chronicle.EventSequences.EventSequencesStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.ObserverDefinitions, CreateResilientStorageFor<Cratis.Chronicle.Observation.ObserverDefinitionGrainStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.ObserverState, CreateResilientStorageFor<Cratis.Chronicle.Observation.ObserverStateGrainStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.FailedPartitions, CreateResilientStorageFor<Cratis.Chronicle.Observation.FailedPartitionGrainStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.Jobs, CreateResilientStorageFor<Cratis.Chronicle.Jobs.JobGrainStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.JobSteps, CreateResilientStorageFor<Cratis.Chronicle.Jobs.JobStepGrainStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.Recommendations, CreateResilientStorageFor<Cratis.Chronicle.Recommendations.RecommendationGrainStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.Projections, CreateResilientStorageFor<Cratis.Chronicle.Projections.ProjectionDefinitionStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.ProjectionsManager, CreateResilientStorageFor<Cratis.Chronicle.Projections.ProjectionsManagerStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.Webhooks, CreateResilientStorageFor<Cratis.Chronicle.Observation.Webhooks.WebhookDefinitionStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.WebhooksManager, CreateResilientStorageFor<Cratis.Chronicle.Observation.Webhooks.WebhooksStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.ProjectionFutures, CreateResilientStorageFor<Cratis.Chronicle.Projections.ProjectionFuturesStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.Reactors, CreateResilientStorageFor<Cratis.Chronicle.Observation.Reactors.ReactorDefinitionStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.Reducers, CreateResilientStorageFor<Cratis.Chronicle.Observation.Reducers.Clients.ReducerDefinitionStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.ReducersManager, CreateResilientStorageFor<Cratis.Chronicle.Observation.Reducers.Clients.ReducersManagerStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.Constraints, CreateResilientStorageFor<Cratis.Chronicle.Events.Constraints.ConstraintsStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.ReadModels, CreateResilientStorageFor<Cratis.Chronicle.ReadModels.ReadModelDefinitionStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.ReadModelsManager, CreateResilientStorageFor<Cratis.Chronicle.ReadModels.ReadModelsManagerStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.ReadModelReplayManager, CreateResilientStorageFor<Cratis.Chronicle.ReadModels.ReadModelReplayManagerStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.EventSeeding, CreateResilientStorageFor<Cratis.Chronicle.Seeding.EventSeedingGrainStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.DataProtectionKeys, CreateResilientStorageFor<Cratis.Chronicle.Security.DataProtectionKeysStorageProvider>);
            services.AddKeyedSingleton(WellKnownGrainStorageProviders.PatchManager, CreateResilientStorageFor<Cratis.Chronicle.Patching.PatchManagerStorageProvider>);
        });

        return builder;
    }

#pragma warning disable CA1859
    static IGrainStorage CreateResilientStorageFor<TStorage>(IServiceProvider serviceProvider, object? context)
#pragma warning restore CA1859
        where TStorage : class, IGrainStorage
        => new ResilientGrainStorage(serviceProvider.GetRequiredService<TStorage>(), serviceProvider.GetRequiredService<ResiliencePipelineProvider<string>>());
}
