// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues;

/// <summary>
/// Checks fresh erasure fences, sharing only in-flight reads within one release operation.
/// </summary>
public static class ReleaseKeys
{
    static readonly AsyncLocal<Memo?> _current = new();

    /// <summary>
    /// Begin a document, batch, or watch update. Nested operations share its fence reads.
    /// </summary>
    /// <returns>A scope that forgets all fences on completion.</returns>
    public static IDisposable BeginScope()
    {
        var previous = _current.Value;
        _current.Value ??= new Memo();
        return new Scope(previous);
    }

    /// <summary>
    /// Get the latest key only if its revision and material are not fenced.
    /// </summary>
    /// <param name="storage">The key store.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="ns">The namespace.</param>
    /// <param name="identifier">The key identifier.</param>
    /// <returns>The usable key, or null when release must be refused.</returns>
    public static async Task<EncryptionKey?> TryGetForRelease(IEncryptionKeyStorage storage, EventStoreName eventStore, EventStoreNamespaceName ns, EncryptionKeyIdentifier identifier)
    {
        var pair = await storage.TryGetLatestFor(eventStore, ns, identifier);
        var fence = await GetErasureForRelease(storage, eventStore, ns, identifier);
        return pair is null || fence.Refuses(pair) ? null : pair.Key;
    }

    /// <summary>
    /// Get a releasable key, treating an unreadable fence as an empty protected value with a diagnostic.
    /// </summary>
    /// <param name="storage">The key store.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="ns">The namespace.</param>
    /// <param name="identifier">The key identifier.</param>
    /// <returns>The usable key, or null when release must be refused.</returns>
    public static async Task<EncryptionKey?> TryGetForReleaseOrEmpty(IEncryptionKeyStorage storage, EventStoreName eventStore, EventStoreNamespaceName ns, EncryptionKeyIdentifier identifier)
    {
        try
        {
            return await TryGetForRelease(storage, eventStore, ns, identifier);
        }
        catch (EncryptionKeyFenceUnavailable)
        {
            Trace.TraceWarning("Encryption key fence unavailable; releasing an empty protected value.");
            return null;
        }
    }

    /// <summary>
    /// Read a release fence, single-flight within the current operation and storage instance only.
    /// </summary>
    /// <param name="storage">The key store.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="ns">The namespace.</param>
    /// <param name="identifier">The key identifier.</param>
    /// <returns>The current erasure fence.</returns>
    public static Task<EncryptionKeyErasure?> GetErasureForRelease(IEncryptionKeyStorage storage, EventStoreName eventStore, EventStoreNamespaceName ns, EncryptionKeyIdentifier identifier)
    {
        var memo = _current.Value;
        if (memo is null)
        {
            return ReadFence(storage, eventStore, ns, identifier);
        }

        var stores = memo.Stores.GetOrAdd(storage, _ => new());
        return stores.GetOrAdd(
            (eventStore, ns, identifier),
            static (scope, store) => new(() => ReadFence(store, scope.EventStore, scope.Namespace, scope.Identifier)),
            storage).Value;
    }

    static async Task<EncryptionKeyErasure?> ReadFence(IEncryptionKeyStorage storage, EventStoreName eventStore, EventStoreNamespaceName ns, EncryptionKeyIdentifier identifier)
    {
        try
        {
            return await storage.GetErasureForRelease(eventStore, ns, identifier);
        }
        catch (Exception error) when (error is not EncryptionKeyFenceUnavailable)
        {
            throw new EncryptionKeyFenceUnavailable(error);
        }
    }

    sealed class Memo
    {
        public ConcurrentDictionary<IEncryptionKeyStorage, ConcurrentDictionary<(EventStoreName EventStore, EventStoreNamespaceName Namespace, EncryptionKeyIdentifier Identifier), Lazy<Task<EncryptionKeyErasure?>>>> Stores { get; } = new(ReferenceEqualityComparer.Instance);
    }

    sealed class Scope(Memo? previous) : IDisposable
    {
        public void Dispose() => _current.Value = previous;
    }
}
