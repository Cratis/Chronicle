// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage.Namespaces;
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
/// <param name="namespaceCasing">The namespace casing cache.</param>
/// <param name="incarnations">The optional opt-in lifecycle routing.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Namespaces)]
public class Namespaces(
    IClusterClient clusterClient,
    ILogger<Namespaces> logger,
    INamespaceCasing namespaceCasing,
    INamespaceIncarnations? incarnations = null) : Grain<NamespacesState>, INamespaces
{
    readonly IBroadcastChannelProvider _namespaceAddedChannel = clusterClient.GetBroadcastChannelProvider(WellKnownBroadcastChannelNames.NamespaceAdded);

    /// <inheritdoc/>
    public Task EnsureDefault() => Ensure(EventStoreNamespaceName.Default);

    /// <inheritdoc/>
    /// <exception cref="NamespaceAddedCouldNotBeAppended">The durable namespace creation notification could not be appended.</exception>
    public async Task Ensure(EventStoreNamespaceName @namespace) => await EnsureCanonical(@namespace);

    /// <inheritdoc/>
    /// <exception cref="NamespaceAddedCouldNotBeAppended">The durable namespace creation notification could not be appended.</exception>
    public async Task<EventStoreNamespaceName> EnsureCanonical(EventStoreNamespaceName @namespace)
    {
        if (incarnations?.Enabled == true) return await incarnations.Ensure(this.GetPrimaryKeyString(), @namespace);
        var existing = await GetCanonical(@namespace);
        if (existing is not null) return existing;

        if (@namespace.Value.Equals(EventStoreNamespaceName.Default.Value, StringComparison.OrdinalIgnoreCase))
        {
            @namespace = EventStoreNamespaceName.Default;
        }
        var eventStoreName = (EventStoreName)this.GetPrimaryKeyString();
        if (NamespaceNameRules.ContainsComma(@namespace.Value))
        {
            throw new InvalidNamespaceName(eventStoreName, @namespace);
        }

        logger.AddingNamespace(@namespace);
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
        return @namespace;
    }

    /// <inheritdoc/>
    public Task<EventStoreNamespaceName?> GetCanonical(EventStoreNamespaceName @namespace)
    {
        if (incarnations?.Enabled == true) return incarnations.GetCanonical(this.GetPrimaryKeyString(), @namespace);
        var existing = State.Namespaces.Concat(State.NewNamespaces)
            .FirstOrDefault(_ => _.Name.Value.Equals(@namespace.Value, StringComparison.OrdinalIgnoreCase));
        if (existing is null) return Task.FromResult<EventStoreNamespaceName?>(null);
        var canonical = existing.Name.Value.Equals(EventStoreNamespaceName.Default.Value, StringComparison.OrdinalIgnoreCase)
            ? EventStoreNamespaceName.Default : existing.Name;
        return Task.FromResult<EventStoreNamespaceName?>(canonical);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<EventStoreNamespaceName>> GetAll() => incarnations?.Enabled == true
        ? incarnations.GetAll(this.GetPrimaryKeyString())
        : Task.FromResult(State.Namespaces.Select(_ => _.Name).ToArray().AsEnumerable());

    /// <inheritdoc/>
    public async Task Forget(EventStoreNamespaceName @namespace)
    {
        var existing = State.Namespaces.FirstOrDefault(_ => _.Name.Value.Equals(@namespace.Value, StringComparison.OrdinalIgnoreCase));
        if (existing is null) return;

        var eventStoreName = (EventStoreName)this.GetPrimaryKeyString();
        State.Namespaces.Remove(existing);
        namespaceCasing.Forget(eventStoreName, existing.Name);

        await RecordRemoval(eventStoreName, @namespace);
    }

    /// <inheritdoc/>
    /// <exception cref="NamespaceIncarnationUnavailable">The guarded callback is unavailable without opt-in.</exception>
    /// <exception cref="NamespaceCannotBeRemoved">The default namespace cannot be forgotten by lifecycle removal.</exception>
    public async Task ForgetIncarnation(EventStoreNamespaceName @namespace, NamespaceIncarnationId incarnation, Guid operation)
    {
        var eventStore = (EventStoreName)this.GetPrimaryKeyString();
        if (incarnations?.Enabled != true) throw new NamespaceIncarnationUnavailable(eventStore);
        if (@namespace.Value.Equals(EventStoreNamespaceName.Default.Value, StringComparison.OrdinalIgnoreCase)) throw new NamespaceCannotBeRemoved(eventStore, @namespace);
        await incarnations.ValidateForget(eventStore, @namespace, incarnation, operation);
        var existing = State.Namespaces.FirstOrDefault(_ => _.Name.Value.Equals(@namespace.Value, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) State.Namespaces.Remove(existing);
        namespaceCasing.Forget(eventStore, @namespace);
        await RecordRemoval(eventStore, @namespace);
    }

    async Task RecordRemoval(EventStoreName eventStoreName, EventStoreNamespaceName @namespace)
    {
        // Recorded once the namespace is gone, so a retry of a half-done removal is not reported as done.
        var result = await GrainFactory.GetSystemEventSequence().Append($"{eventStoreName}/{@namespace}", new NamespaceRemoved(eventStoreName, @namespace));
        if (!result.IsSuccess)
        {
            logger.CouldNotRecordNamespaceRemoval(@namespace);
        }
    }
}
