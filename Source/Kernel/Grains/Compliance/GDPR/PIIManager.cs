// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance;
using Aksio.Cratis.Kernel.Compliance;
using Aksio.Cratis.Kernel.Persistence.Compliance;

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
        var key = _encryption.GenerateKey();
        await _keyStore.SaveFor(identifier, key);
    }

    /// <inheritdoc/>
    public Task DeleteEncryptionKeyFor(EncryptionKeyIdentifier identifier) => _keyStore.DeleteFor(identifier);
}
