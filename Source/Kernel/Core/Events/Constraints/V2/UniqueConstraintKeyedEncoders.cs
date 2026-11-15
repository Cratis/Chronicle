// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Security.Cryptography;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Events.Constraints.V2;

/// <summary>
/// Loads and caches one <see cref="UniqueConstraintKeyedEncoder"/> per event store from the stored encoding key.
/// </summary>
/// <remarks>
/// This never creates a key: only the offline cutover provisions keys. Absence and failures are never cached, so they
/// are retried on the next call. Concurrent callers share one in-flight load.
/// </remarks>
/// <param name="storage">The <see cref="IStorage"/> used to read each event store's key.</param>
internal sealed class UniqueConstraintKeyedEncoders(IStorage storage)
{
    readonly ConcurrentDictionary<EventStoreName, Lazy<Task<UniqueConstraintKeyedEncoder?>>> _encoders = new();

    /// <summary>
    /// Gets the encoder for an event store when its key exists.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to get the encoder for.</param>
    /// <returns>The shared encoder, or null when the event store has no key.</returns>
    public async ValueTask<UniqueConstraintKeyedEncoder?> TryGetFor(EventStoreName eventStore)
    {
        var entry = _encoders.GetOrAdd(eventStore, static (name, owner) => new Lazy<Task<UniqueConstraintKeyedEncoder?>>(() => owner.Load(name)), this);
        try
        {
            var encoder = await entry.Value;
            if (encoder is null)
            {
                Forget(eventStore, entry);
            }

            return encoder;
        }
        catch
        {
            Forget(eventStore, entry);
            throw;
        }
    }

    async Task<UniqueConstraintKeyedEncoder?> Load(EventStoreName eventStore)
    {
        var key = await storage.GetEventStore(eventStore).UniqueConstraintEncoding.GetKey();
        if (key is null)
        {
            return null;
        }

        var material = key.Material.CopyBytes();
        try
        {
            return new UniqueConstraintKeyedEncoder(material);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(material);
        }
    }

    void Forget(EventStoreName eventStore, Lazy<Task<UniqueConstraintKeyedEncoder?>> entry) =>
        _encoders.TryRemove(new KeyValuePair<EventStoreName, Lazy<Task<UniqueConstraintKeyedEncoder?>>>(eventStore, entry));
}
