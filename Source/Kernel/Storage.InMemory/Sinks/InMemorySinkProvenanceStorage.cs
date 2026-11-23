// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.Sinks;

/// <summary>
/// Represents the process-lifetime provenance of the in-memory provider's logical database.
/// </summary>
/// <param name="eventStore">The event store.</param>
/// <param name="namespace">The namespace.</param>
public class InMemorySinkProvenanceStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace) : ISinkProvenanceStorage
{
    static readonly ConcurrentDictionary<(EventStoreName, EventStoreNamespaceName), bool> _markers = new();

    /// <inheritdoc/>
    public Task<bool> IsQuarantined() => Task.FromResult(_markers.ContainsKey((eventStore, @namespace)));

    /// <inheritdoc/>
    public Task MarkQuarantined()
    {
        _markers.TryAdd((eventStore, @namespace), true);
        return Task.CompletedTask;
    }
}
