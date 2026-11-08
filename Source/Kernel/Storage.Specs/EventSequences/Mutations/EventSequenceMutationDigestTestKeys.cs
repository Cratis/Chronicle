// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Provides deterministic per-event-store digest keys for specs that compute keyed digests without a registry.
/// </summary>
public static class EventSequenceMutationDigestTestKeys
{
    static readonly ConcurrentDictionary<EventStoreName, EventSequenceMutationDigestKey> _keys = new();

    /// <summary>
    /// Gets the spec key of an event store.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <returns>The key, derived from a secret that is distinct per event store.</returns>
    public static EventSequenceMutationDigestKey For(EventStoreName eventStore) =>
        _keys.GetOrAdd(eventStore, static name => EventSequenceMutationDigestKey.DeriveFrom(name, SecretFor(name)));

    /// <summary>
    /// Gets the spec secret of an event store.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <returns>The 32-byte secret.</returns>
    public static byte[] SecretFor(EventStoreName eventStore) => SHA256.HashData(Encoding.UTF8.GetBytes($"spec-secret:{eventStore.Value}"));

    /// <summary>
    /// Makes the spec keys available to every flow without a key in scope.
    /// </summary>
    public static void Install() => EventSequenceMutationDigestSeam.UseKeysForAllFlowsForTest(For);
}
