// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance;
using Aksio.Cratis.Kernel.Compliance;
using Aksio.Cratis.Kernel.Storage.Compliance;

namespace Aksio.Cratis.Kernel.Grains.Compliance.GDPR;

/// <summary>
/// Represents a manager of PII in the system.
/// </summary>
public class PIIManager : Grain, IPIIManager
{
    readonly IEncryption _encryption;
    readonly IEncryptionKeyStorage _keyStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="PIIManager"/> class.
    /// </summary>
    /// <param name="encryption"><see cref="IEncryption"/> system.</param>
    /// <param name="keyStore">The <see cref="IEncryptionKeyStorage"/>.</param>
    public PIIManager(IEncryption encryption, IEncryptionKeyStorage keyStore)
    {
        _encryption = encryption;
        _keyStore = keyStore;
    }

    /// <inheritdoc/>
    public async Task CreateAndRegisterKeyFor(EncryptionKeyIdentifier identifier)
    {
        _ = this.GetPrimaryKey(out var primaryKeyExtension);
        var primaryKey = (PIIManagerKey)primaryKeyExtension;

        var key = _encryption.GenerateKey();
        await _keyStore.SaveFor((string)primaryKey.MicroserviceId, primaryKey.TenantId, identifier, key);
    }

    /// <inheritdoc/>
    public async Task DeleteEncryptionKeyFor(EncryptionKeyIdentifier identifier)
    {
        _ = this.GetPrimaryKey(out var primaryKeyExtension);
        var primaryKey = (PIIManagerKey)primaryKeyExtension;

        await _keyStore.DeleteFor((string)primaryKey.MicroserviceId, primaryKey.TenantId, identifier);
    }
}
