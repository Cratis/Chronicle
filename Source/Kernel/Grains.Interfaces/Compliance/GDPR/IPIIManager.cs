// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance;

namespace Aksio.Cratis.Kernel.Grains.Compliance.GDPR;

/// <summary>
/// Represents a manager of PII in the system.
/// </summary>
public interface IPIIManager : IGrainWithGuidKey
{
    /// <summary>
    /// Creates a new encryption key and registers it for the specific identifier.
    /// </summary>
    /// <param name="identifier"><see cref="EncryptionKeyIdentifier"/> to register for.</param>
    /// <returns>Awaitable task.</returns>
    Task CreateAndRegisterKeyFor(EncryptionKeyIdentifier identifier);

    /// <summary>
    /// Deletes a specific encryption key based on th e<see cref="EncryptionKeyIdentifier"/>.
    /// </summary>
    /// <param name="identifier"><see cref="EncryptionKeyIdentifier"/> to delete.</param>
    /// <returns>Awaitable task.</returns>
    Task DeleteEncryptionKeyFor(EncryptionKeyIdentifier identifier);
}
