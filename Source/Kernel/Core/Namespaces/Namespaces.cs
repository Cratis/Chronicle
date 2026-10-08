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
        if (State.Namespaces.Any(_ => _.Name.Value.Equals(@namespace.Value, StringComparison.InvariantCultureIgnoreCase))) return;

        logger.AddingNamespace(@namespace);
        var eventStoreName = (EventStoreName)this.GetPrimaryKeyString();
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
}
