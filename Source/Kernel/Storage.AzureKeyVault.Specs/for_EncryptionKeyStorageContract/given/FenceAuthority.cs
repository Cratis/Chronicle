// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_EncryptionKeyStorageContract.given;

public class FenceAuthority : IEncryptionKeyFence, IEncryptionKeyFenceRecords
{
    readonly InMemoryEncryptionKeyStorage _storage = new();

    public bool IsDurable { get; set; } = true;
    public bool Unavailable { get; set; }
    public bool LoseResponse { get; set; }
    public Func<Task>? BeforeErasure { get; set; }

    public Task<string?> ReadRecord(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string id) =>
        _storage.ReadRecord(eventStore, eventStoreNamespace, id);

    public Task WriteRecord(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string id, string value, bool replace = false) =>
        _storage.WriteRecord(eventStore, eventStoreNamespace, id, value, replace);

    public Task<EncryptionKeyFenceState> ReadFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        Unavailable ? throw new EncryptionKeyFenceUnavailable(identifier) : _storage.ReadFor(eventStore, eventStoreNamespace, identifier);

    public async Task<bool> CompareExchangeFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyFenceState observed, EncryptionKeyFenceState next)
    {
        if (Unavailable) throw new EncryptionKeyFenceUnavailable(identifier);
        if ((next.Erasure.Requests?.Count ?? 0) > (observed.Erasure.Requests?.Count ?? 0) && BeforeErasure is { } competing)
        {
            BeforeErasure = null;
            await competing();
        }

        var result = await _storage.CompareExchangeFor(eventStore, eventStoreNamespace, identifier, observed, next);
        if (result && LoseResponse)
        {
            LoseResponse = false;
            throw new TimeoutException("Applied authority write lost its response");
        }

        return result;
    }
}
