// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelGrpc;

using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Integration.Clustering.for_Occurrences;

public class OccurrenceFixture : ClusteringFixture
{
    public FanOutCheckpoint FanOut { get; } = new();
    public bool HasTwoSilos => SiloCount == 2;
    protected override Task WarmUpAsync() => Task.CompletedTask;

    protected override void ConfigureFixtureServices(IServiceCollection services)
    {
        var descriptor = services.Last(service => service.ServiceType == typeof(IStorage));
        services.AddSingleton<IStorage>(provider =>
        {
            var storage = descriptor.ImplementationInstance as IStorage
                ?? descriptor.ImplementationFactory?.Invoke(provider) as IStorage
                ?? (IStorage)ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType!);
            return StorageObservationProxy.Wrap(storage, (name, value) => name == nameof(IStorage.GetEventStore)
                ? StorageObservationProxy.Wrap((IEventStoreStorage)value!, (member, result) => member == "get_LifecycleFanOut"
                    ? FanOut.Decorate((ILifecycleFanOutStorage)result!) : result)
                : value);
        });

        // The legacy in-process composite predates this generated service. Use the actual implementation,
        // constructed by the real silo's DI container, rather than a successful stub or a capability override.
        var serviceDescriptor = services.Last(service => service.ServiceType == typeof(Contracts.IServices));
        services.AddSingleton<Contracts.IServices>(provider =>
        {
            var existing = (Contracts.Services)serviceDescriptor.ImplementationFactory!(provider);
            var implementation = typeof(KernelGrpc::Orleans.Hosting.ChronicleServerSiloBuilderExtensions).Assembly
                .GetType("Cratis.Chronicle.Services.Occurrences.Occurrences", throwOnError: true)!;
            return existing with
            {
                Occurrences = (Contracts.Occurrences.IOccurrences)ActivatorUtilities.CreateInstance(provider, implementation)
            };
        });
    }

    public async Task Rehydrate()
    {
        var factory = SiloServices.GetRequiredService<IGrainFactory>();
        var store = SiloServices.GetRequiredService<IStorage>().GetEventStore(new(Constants.EventStore));
        await factory.GetGrain<KernelCore::Cratis.Chronicle.Occurrences.ILifecycleOccurrences>(Constants.EventStore).Ensure();
        await KernelCore::Cratis.Chronicle.Occurrences.OccurrenceRecovery.Rehydrate(factory, store.Occurrences, new(Constants.EventStore), null, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
        foreach (var state in await store.Namespaces.GetAll())
        {
            await KernelCore::Cratis.Chronicle.Occurrences.OccurrenceRecovery.Rehydrate(factory, store.GetNamespace(state.Name).Occurrences, new(Constants.EventStore), state.Name.Value, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
        }
    }

    public IEventStoreStorage Storage => SiloServices.GetRequiredService<IStorage>().GetEventStore(new(Constants.EventStore));
}
