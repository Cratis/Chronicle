// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR;

/// <summary>
/// Represents a manager of PII in the system.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PIIManager"/> class.
/// </remarks>
/// <param name="keyStore">The <see cref="IEncryptionKeyStorage"/>.</param>
/// <param name="cacheClient">The <see cref="IEncryptionKeyCacheClient"/> used to evict the key from every silo's cache.</param>
public class PIIManager(IEncryptionKeyStorage keyStore, IEncryptionKeyCacheClient cacheClient) : Grain, IPIIManager
{
    /// <inheritdoc/>
    public async Task DeleteEncryptionKeyFor(EncryptionKeyIdentifier identifier)
    {
        _ = this.GetPrimaryKey(out var primaryKeyExtension);
        var primaryKey = (PIIManagerKey)primaryKeyExtension!;

        try
        {
            await keyStore.DeleteFor(primaryKey.EventStore, primaryKey.Namespace, identifier);
        }
        finally
        {
            // The eviction is not conditional on the erase succeeding. A composite key store attempts every store and
            // then reports a partial failure, so the key can be durably destroyed and the call still throw - and a
            // throw that skipped the eviction would leave every peer silo serving the erased key from a cache that
            // has no time-to-live and nothing else to clear it. Evicting is idempotent and cheap, so it always runs.
            await cacheClient.Evict(primaryKey.EventStore, primaryKey.Namespace, identifier);
        }
    }
}
