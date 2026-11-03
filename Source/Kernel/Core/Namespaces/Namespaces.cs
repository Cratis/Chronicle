// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Naming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.BroadcastChannel;
using Orleans.Providers;

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// Represents an implementation of <see cref="INamespaces"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Namespaces"/> class.
/// </remarks>
/// <param name="clusterClient"><see cref="IClusterClient"/> instance.</param>
/// <param name="logger"><see cref="ILogger{TCategoryName}"/> instance.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Namespaces)]
public class Namespaces(
    IClusterClient clusterClient,
    ILogger<Namespaces> logger) : Grain<NamespacesState>, INamespaces
{
    readonly IBroadcastChannelProvider _namespaceAddedChannel = clusterClient.GetBroadcastChannelProvider(WellKnownBroadcastChannelNames.NamespaceAdded);

    /// <inheritdoc/>
    public Task EnsureDefault() => Ensure(EventStoreNamespaceName.Default);

    /// <inheritdoc/>
    /// <exception cref="NamespaceAddedCouldNotBeAppended">The durable namespace creation notification could not be appended.</exception>
    public async Task Ensure(EventStoreNamespaceName @namespace)
    {
        // Only the exact registered name is the same namespace; a case variant is a new name and goes through the gate.
        if (State.Namespaces.Any(_ => _.Name.Value.Equals(@namespace.Value, StringComparison.Ordinal))) return;

        logger.AddingNamespace(@namespace);
        var eventStoreName = (EventStoreName)this.GetPrimaryKeyString();
        await EnsureCanCreate(eventStoreName, @namespace);
        var added = new NamespaceAdded(eventStoreName, @namespace);

        // Record the notification before committing the namespace. Otherwise a failed append followed by
        // Ensure would find an existing namespace and permanently skip global seeding. Retried notifications
        // are safe because each namespace's seeding grain tracks the entries it has already appended.
        var result = await GrainFactory.GetSystemEventSequence().Append($"{eventStoreName}/{@namespace}", added);
        if (!result.IsSuccess)
        {
            throw new NamespaceAddedCouldNotBeAppended(eventStoreName, @namespace);
        }

        State.NewNamespaces.Add(new NamespaceState(@namespace, DateTimeOffset.UtcNow));
        var channelId = ChannelId.Create(WellKnownBroadcastChannelNames.NamespaceAdded, eventStoreName);
        await WriteStateAsync();

        logger.BroadcastAddedNamespace(@namespace);

        var channelWriter = _namespaceAddedChannel.GetChannelWriter<NamespaceAdded>(channelId);
        await channelWriter.Publish(added);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<EventStoreNamespaceName>> GetAll() =>
        Task.FromResult(State.Namespaces.Select(_ => _.Name).ToArray().AsEnumerable());

    /// <inheritdoc/>
    public async Task Forget(EventStoreNamespaceName @namespace)
    {
        var existing = State.Namespaces.FirstOrDefault(_ => _.Name.Value.Equals(@namespace.Value, StringComparison.Ordinal));
        if (existing is null) return;

        var eventStoreName = (EventStoreName)this.GetPrimaryKeyString();
        State.Namespaces.Remove(existing);

        // Recorded once the namespace is gone, so a retry of a half-done removal is not reported as done.
        var result = await GrainFactory.GetSystemEventSequence().Append($"{eventStoreName}/{@namespace}", new NamespaceRemoved(eventStoreName, @namespace));
        if (!result.IsSuccess)
        {
            logger.CouldNotRecordNamespaceRemoval(@namespace);
        }
    }

    async Task EnsureCanCreate(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        var gate = ServiceProvider.GetService<IResourceNameGate>();
        if (gate is null)
        {
            return;
        }

        var clusterStorage = ServiceProvider.GetService<IClusterStorage>();
        var eventStores = clusterStorage is null ? [] : (await clusterStorage.GetEventStores()).Select(_ => _.Value).ToArray();
        await gate.EnsureCanCreate(new ResourceCreation(
            eventStore,
            @namespace,
            State.Namespaces.Select(_ => _.Name.Value).ToArray(),
            eventStores));
    }
}
