// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Security.Cryptography;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Represents an implementation of <see cref="IEventSequenceMutationDigestKeys"/> backed by the encryption key store.
/// </summary>
/// <remarks>
/// <para>
/// Each event store has one secret, stored under <see cref="EventSequenceMutationDigestKeyIdentifiers.Secret"/> in the
/// default namespace. It is created on first use through <see cref="IEncryptionKeyStorage.GetOrAddFor"/>, the key store's
/// create-if-absent operation, which returns the stored key rather than the candidate when another silo won the race.
/// Every silo therefore adopts the same secret.
/// </para>
/// <para>
/// The derived key is cached per event store for the life of the process, so a mutation does not read the key store.
/// A failure is not cached: the next operation tries again, and until then the caller fails closed.
/// </para>
/// </remarks>
/// <param name="keyStorage">The encryption key store the secret lives in.</param>
/// <param name="logger">The logger.</param>
[Singleton]
public class EventSequenceMutationDigestKeys(IEncryptionKeyStorage keyStorage, ILogger<EventSequenceMutationDigestKeys> logger) : IEventSequenceMutationDigestKeys
{
    readonly ConcurrentDictionary<EventStoreName, EventSequenceMutationDigestKey> _keys = new();

    /// <inheritdoc/>
    public async Task<EventSequenceMutationDigestKey?> TryGetFor(EventStoreName eventStore)
    {
        if (_keys.TryGetValue(eventStore, out var cached))
        {
            return cached;
        }

        try
        {
            var candidate = new EncryptionKey(
                RandomNumberGenerator.GetBytes(EventSequenceMutationDigestKey.MinimumSecretLength),
                RandomNumberGenerator.GetBytes(EventSequenceMutationDigestKey.MinimumSecretLength));
            var stored = await keyStorage.GetOrAddFor(
                eventStore,
                EventSequenceMutationDigestKeyIdentifiers.Namespace,
                EventSequenceMutationDigestKeyIdentifiers.Secret,
                candidate);
            return _keys.GetOrAdd(eventStore, static (name, secret) => EventSequenceMutationDigestKey.DeriveFrom(name, secret), stored.Private);
        }
        catch (Exception ex)
        {
            // Neither the key store's failures nor the derivation's carry key material, so the exception is safe to log.
            logger.DigestKeyUnavailable(ex, eventStore);
            return null;
        }
    }
}
