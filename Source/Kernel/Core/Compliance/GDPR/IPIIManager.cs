// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR;

/// <summary>
/// Represents a manager of PII in the system.
/// </summary>
public interface IPIIManager : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Deletes a specific encryption key based on the <see cref="EncryptionKeyIdentifier"/>.
    /// </summary>
    /// <param name="identifier"><see cref="EncryptionKeyIdentifier"/> to delete.</param>
    /// <returns>Awaitable task.</returns>
    Task DeleteEncryptionKeyFor(EncryptionKeyIdentifier identifier);
}
