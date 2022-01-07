// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans;

namespace Cratis.Compliance.Grains
{
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
    }
}
