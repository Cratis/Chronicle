// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Orleans.Runtime.Services;

namespace Cratis.Chronicle.Compliance;

/// <summary>
/// Represents an implementation of <see cref="IEncryptionKeyCacheClient"/> that fans out to the
/// <see cref="IEncryptionKeyCacheGrainService"/> on every silo, so each silo evicts its own encryption key cache.
/// </summary>
/// <remarks>
/// <para>
/// Initializes a new instance of the <see cref="EncryptionKeyCacheClient"/> class.
/// </para>
/// <para>
/// The fan-out is awaited, so the caller only observes completion once every silo has acknowledged the eviction.
/// This is the acknowledged-delivery guarantee crypto-shred relies on: a deleted key can no longer decrypt on any peer.
/// </para>
/// </remarks>
/// <param name="grainFactory"><see cref="IGrainFactory"/> to use for getting grains.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting services.</param>
public class EncryptionKeyCacheClient(IGrainFactory grainFactory, IServiceProvider serviceProvider)
    : GrainServiceClient<IEncryptionKeyCacheGrainService>(serviceProvider), IEncryptionKeyCacheClient
{
    readonly IManagementGrain _managementGrain = grainFactory.GetGrain<IManagementGrain>(1);

    /// <inheritdoc/>
    public async Task Evict(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        await ForEachGrainService(service => service.Evict(eventStore, eventStoreNamespace, identifier));

    async Task ForEachGrainService(Func<IEncryptionKeyCacheGrainService, Task> callback)
    {
        var hosts = await _managementGrain.GetHosts(true);
        var tasks = hosts.Keys.Select(host => callback(GetGrainService(host)));
        await Task.WhenAll(tasks);
    }
}
