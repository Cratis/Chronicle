// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>Durable metadata, isolated from key material and authoritative fence rows.</summary>
public interface IEncryptionKeyFenceRecords
{
    /// <summary>Count fences that still have legacy metadata rather than a protocol-N seal.</summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="eventStoreNamespace">The namespace.</param>
    /// <returns>The legacy fence count, or null when unsupported.</returns>
    Task<long?> CountLegacyFences(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace) => Task.FromResult<long?>(null);

    /// <summary>Read an acknowledged metadata record from the primary.</summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="eventStoreNamespace">The namespace.</param>
    /// <param name="id">The metadata identity.</param>
    /// <returns>The record, or null.</returns>
    Task<string?> ReadRecord(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string id);

    /// <summary>Write metadata, insert-only unless explicitly replacing a silo heartbeat.</summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="eventStoreNamespace">The namespace.</param>
    /// <param name="id">The metadata identity.</param>
    /// <param name="value">The serialized record.</param>
    /// <param name="replace">Whether to replace an existing heartbeat.</param>
    /// <returns>Awaitable write.</returns>
    Task WriteRecord(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string id, string value, bool replace = false);
}
