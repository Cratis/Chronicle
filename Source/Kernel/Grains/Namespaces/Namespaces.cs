// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;
using Microsoft.Extensions.Logging;
using Orleans.BroadcastChannel;
using Orleans.Providers;

namespace Cratis.Chronicle.Grains.Namespaces;

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
    public async Task Ensure(EventStoreNamespaceName @namespace)
    {
        if (State.Namespaces.Any(_ => _.Name.Value.Equals(@namespace.Value, StringComparison.InvariantCultureIgnoreCase))) return;

        logger.AddingNamespace(@namespace);
        State.NewNamespaces.Add(new NamespaceState(@namespace, DateTimeOffset.UtcNow));

        var eventStoreName = (EventStoreName)this.GetPrimaryKeyString();
        var channelId = ChannelId.Create(WellKnownBroadcastChannelNames.NamespaceAdded, eventStoreName);
        await WriteStateAsync();

        logger.BroadcastAddedNamespace(@namespace);

        var channelWriter = _namespaceAddedChannel.GetChannelWriter<NamespaceAdded>(channelId);
        var eventStore = this.GetPrimaryKeyString();
        await channelWriter.Publish(new NamespaceAdded(eventStore, @namespace));
    }

    /// <inheritdoc/>
    public Task<IEnumerable<EventStoreNamespaceName>> GetAll() =>
        Task.FromResult(State.Namespaces.Select(_ => _.Name).ToArray().AsEnumerable());
}
