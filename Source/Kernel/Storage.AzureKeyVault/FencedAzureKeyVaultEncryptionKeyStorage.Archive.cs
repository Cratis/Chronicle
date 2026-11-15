// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault;

internal sealed partial class FencedAzureKeyVaultEncryptionKeyStorage
{
    /// <inheritdoc/>
    public async Task<EncryptionKeyErasureRequest?> GetErasureRequestFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, string requestId)
    {
        var address = await AddressFor(eventStore, eventStoreNamespace, identifier);
        var observed = await ReadState(address);
        return observed.Erasure.Requests?.SingleOrDefault(_ => _.RequestId == requestId) ?? await Archived(address, requestId);
    }

    /// <inheritdoc/>
    public async Task CompactErasureRequestsFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, int retain = 32)
    {
        if (authority is not IEncryptionKeyFenceRecords records) throw new EncryptionKeyFenceUnavailable(identifier);
        var address = await AddressFor(eventStore, eventStoreNamespace, identifier);
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var observed = await ReadState(address);
            var compacted = await EncryptionKeyRequestArchive.Prepare(records, eventStore, eventStoreNamespace, address.AuthorityIdentifier, observed.Erasure, retain);
            if (await authority.CompareExchangeFor(eventStore, eventStoreNamespace, address.AuthorityIdentifier, observed, observed with { Erasure = compacted })) return;
            await Backoff(attempt);
        }

        throw new EncryptionKeyFenceUnavailable(identifier);
    }

    Task<EncryptionKeyErasureRequest?> Archived(Address address, string requestId) => authority is IEncryptionKeyFenceRecords records
        ? EncryptionKeyRequestArchive.Read(records, address.EventStore, address.Namespace, address.AuthorityIdentifier, requestId)
        : Task.FromResult<EncryptionKeyErasureRequest?>(null);
}
