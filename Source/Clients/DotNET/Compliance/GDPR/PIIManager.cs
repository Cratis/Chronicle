// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR;

/// <summary>
/// Represents an implementation of <see cref="IPIIManager"/>.
/// </summary>
public class PIIManager : IPIIManager
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PIIManager"/> class.
    /// </summary>
    public PIIManager()
    {
    }

    /// <inheritdoc/>
    public Task CreateAndRegisterKeyFor(EncryptionKeyIdentifier identifier) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task DeleteEncryptionKeyFor(EncryptionKeyIdentifier identifier) => throw new NotImplementedException();
}
