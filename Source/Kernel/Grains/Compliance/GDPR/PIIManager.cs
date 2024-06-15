// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Compliance;

namespace Cratis.Chronicle.Grains.Compliance.GDPR;

/// <summary>
/// Represents a manager of PII in the system.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PIIManager"/> class.
/// </remarks>
/// <param name="encryption"><see cref="IEncryption"/> system.</param>
/// <param name="keyStore">The <see cref="IEncryptionKeyStorage"/>.</param>
public class PIIManager(IEncryption encryption, IEncryptionKeyStorage keyStore) : Grain, IPIIManager
{
    /// <inheritdoc/>
    public async Task CreateAndRegisterKeyFor(EncryptionKeyIdentifier identifier)
    {
        _ = this.GetPrimaryKey(out var primaryKeyExtension);
        var primaryKey = (PIIManagerKey)primaryKeyExtension;

        var key = encryption.GenerateKey();
        await keyStore.SaveFor(primaryKey.EventStore, primaryKey.Namespace, identifier, key);
    }

    /// <inheritdoc/>
    public async Task DeleteEncryptionKeyFor(EncryptionKeyIdentifier identifier)
    {
        _ = this.GetPrimaryKey(out var primaryKeyExtension);
        var primaryKey = (PIIManagerKey)primaryKeyExtension;

        await keyStore.DeleteFor(primaryKey.EventStore, primaryKey.Namespace, identifier);
    }
}
