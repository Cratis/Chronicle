// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;

using Cratis.Chronicle;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using DotNet.Testcontainers.Networks;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Storage;
using EventSequencesStorageProvider = KernelCore::Cratis.Chronicle.EventSequences.EventSequencesStorageProvider;
using WellKnownGrainStorageProviders = KernelCore::Cratis.Chronicle.WellKnownGrainStorageProviders;
namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Marks the integration test setup fixture.
/// </summary>
public interface IChronicleSetupFixture : IClientArtifactsProvider
{
    /// <summary>
    /// Gets the docker network.
    /// </summary>
    public INetwork Network { get; }

    /// <summary>
    /// Gets the event store database.
    /// </summary>
    public MongoDBDatabase EventStoreDatabase { get; }

    /// <summary>
    /// Gets the event store database for the namespace used in the event store.
    /// </summary>
    public MongoDBDatabase EventStoreForNamespaceDatabase { get; }

    /// <summary>
    /// Gets the read models database.
    /// </summary>
    public MongoDBDatabase ReadModelsDatabase { get; }

    /// <summary>
    /// Gets the event store.
    /// </summary>
    public IEventStore EventStore { get; }

    /// <summary>
    /// Gets the <see cref="IChronicleClient"/>.
    /// </summary>
    public IChronicleClient ChronicleClient { get; }

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> for resolving services.
    /// </summary>
    public IServiceProvider Services { get; }

    /// <summary>
    /// Gets the <see cref="IGrainFactory"/> for the Orleans silo.
    /// </summary>
    internal IGrainFactory GrainFactory => Services.GetRequiredService<IGrainFactory>();

    /// <summary>
    /// Internal: Gets the <see cref="KernelCore::Cratis.Chronicle.EventSequences.IEventSequence"/> for the event log.
    /// </summary>
    /// <returns>The <see cref="KernelCore::Cratis.Chronicle.EventSequences.IEventSequence"/>.</returns>
    internal KernelCore::Cratis.Chronicle.EventSequences.IEventSequence EventLogSequenceGrain => GetEventSequenceGrain(KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceId.Log);

    /// <summary>
    /// Internal: Gets the <see cref="IEventStoreStorage"/> for the event store.
    /// </summary>
    internal IEventStoreStorage EventStoreStorage => Services.GetRequiredService<IStorage>().GetEventStore(Constants.EventStore);

    /// <summary>
    /// Sets the name of the fixture.
    /// </summary>
    /// <param name="name">Name for the fixture.</param>
    public void SetName(string name);

    /// <summary>
    /// Internal: Gets the <see cref="IEventStoreNamespaceStorage"/> for the event store namespace.
    /// </summary>
    /// <param name="namespaceName">The namespace name.</param>
    /// <returns>The <see cref="IEventStoreNamespaceStorage"/>.</returns>
    internal IEventStoreNamespaceStorage GetEventStoreNamespaceStorage(KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName? namespaceName = null) => EventStoreStorage.GetNamespace(namespaceName ?? KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName.Default);

    /// <summary>
    /// Internal: Gets the <see cref="IEventSequenceStorage"/> for the event log.
    /// </summary>
    /// <param name="namespaceName">The namespace name.</param>
    /// <returns>The <see cref="IEventSequenceStorage"/>.</returns>
    internal IEventSequenceStorage GetEventLogStorage(KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName? namespaceName = null) => GetEventStoreNamespaceStorage(namespaceName).GetEventSequence(KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceId.Log);

    /// <summary>
    /// Gets the <see cref="IGrainStorage"/> for the specified key.
    /// </summary>
    /// <typeparam name="TStorage">The type of the storage.</typeparam>
    /// <param name="key">The key of the storage.</param>
    /// <returns>The <see cref="IGrainStorage"/>.</returns>
    internal TStorage GetGrainStorage<TStorage>(string key)
        where TStorage : IGrainStorage => (TStorage)Services.GetRequiredKeyedService<IGrainStorage>(key);

    /// <summary>
    /// Internal: Gets the <see cref="EventSequencesStorageProvider"/> for the event sequences.
    /// </summary>
    /// <returns>The <see cref="EventSequencesStorageProvider"/>.</returns>
    internal EventSequencesStorageProvider GetEventSequenceStatesStorage() => GetGrainStorage<EventSequencesStorageProvider>(WellKnownGrainStorageProviders.EventSequences);

    /// <summary>
    /// Internal: Gets the <see cref="KernelCore::Cratis.Chronicle.EventSequences.IEventSequence"/> for the specified event sequence ID.
    /// </summary>
    /// <param name="id">The event sequence ID.</param>
    /// <returns>The <see cref="KernelCore::Cratis.Chronicle.EventSequences.IEventSequence"/>.</returns>
    internal KernelCore::Cratis.Chronicle.EventSequences.IEventSequence GetEventSequenceGrain(KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceId id) => Services.GetRequiredService<IGrainFactory>().GetGrain<KernelCore::Cratis.Chronicle.EventSequences.IEventSequence>(CreateEventSequenceKey(id));

    /// <summary>
    /// Internal: Creates an <see cref="KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceKey"/> for the specified event sequence ID.
    /// </summary>
    /// <param name="id">The event sequence ID.</param>
    /// <returns>The <see cref="KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceKey"/>.</returns>
    internal KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceKey CreateEventSequenceKey(KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceId id) => new(id, Constants.EventStore, KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName.Default);
}
