// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;

using Cratis.Chronicle.Storage;
using KernelEventStoreName = KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName;
using KernelNamespaceName = KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Resolves namespace metadata in scenarios without requiring an Orleans namespace grain.
/// </summary>
/// <param name="storage">The scenario storage.</param>
internal sealed class InProcessNamespaceCasing(IStorage storage) : KernelCore::Cratis.Chronicle.Namespaces.INamespaceCasing
{
    /// <inheritdoc/>
    public void Forget(KernelEventStoreName eventStore, KernelNamespaceName @namespace)
    {
        // Resolutions read storage directly; there is no cache to invalidate.
    }

    /// <inheritdoc/>
    public async ValueTask<KernelNamespaceName> Canonicalize(KernelEventStoreName eventStore, KernelNamespaceName @namespace)
    {
        var namespaces = await storage.GetEventStore(eventStore).Namespaces.GetAll();
        return namespaces.FirstOrDefault(_ => _.Name.Value.Equals(@namespace.Value, StringComparison.OrdinalIgnoreCase))?.Name ?? @namespace;
    }
}
