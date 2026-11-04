// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Naming;

namespace Cratis.Chronicle.Storage.InMemory;

/// <summary>
/// Represents a read-only <see cref="IResourceInventory"/> for the in-memory storage.
/// </summary>
/// <remarks>
/// There are no physical resources, so only the owners are reported: the event stores and namespaces that exist, and the System event store.
/// </remarks>
/// <param name="eventStoreStorages">The shared <see cref="EventStoreStorages"/> registry.</param>
public sealed class InMemoryResourceInventory(EventStoreStorages eventStoreStorages) : IResourceInventory
{
    /// <inheritdoc/>
    public async Task<IEnumerable<ResourceUse>> Read(CancellationToken cancellationToken)
    {
        var uses = new List<ResourceUse>
        {
            new(new ResourceOwner(EventStoreName.System.Value), ResourceKind.EventStoreDatabase, $"memory:{EventStoreName.System.Value}")
        };

        foreach (var eventStore in eventStoreStorages.Names)
        {
            var owner = new ResourceOwner(eventStore.Value);
            uses.Add(new(owner, ResourceKind.EventStoreDatabase, $"memory:{eventStore.Value}"));

            var namespaces = await eventStoreStorages.GetOrCreate(eventStore).Namespaces.GetAll();
            uses.AddRange(namespaces.Select(_ => new ResourceUse(new ResourceOwner(eventStore.Value, _.Name.Value), ResourceKind.NamespaceDatabase, $"memory:{eventStore.Value}#{_.Name.Value}")));
        }

        return uses;
    }
}
