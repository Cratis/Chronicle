// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.InMemory.Namespaces;

/// <summary>
/// Stores durable-for-the-lifetime-of-the-store lifecycle tombstones independently of namespace data.
/// </summary>
public sealed class NamespaceLifecycleStorage : INamespaceLifecycleStorage
{
    readonly ConcurrentDictionary<string, NamespaceLifecycle> _entries = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public Task<NamespaceLifecycle?> Get(EventStoreNamespaceName name) =>
        Task.FromResult(_entries.GetValueOrDefault(name.Value));

    /// <inheritdoc/>
    public Task<bool> TryWrite(EventStoreNamespaceName name, long? expectedRevision, NamespaceLifecycle next)
    {
        if (next.Revision != (expectedRevision ?? 0) + 1 ||
            !name.Value.Equals(next.Namespace.Value, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(false);
        }
        if (expectedRevision is null)
        {
            return Task.FromResult(_entries.TryAdd(name.Value, next));
        }
        var won = _entries.TryGetValue(name.Value, out var current) &&
            current.Revision == expectedRevision && _entries.TryUpdate(name.Value, next, current);
        return Task.FromResult(won);
    }
}
