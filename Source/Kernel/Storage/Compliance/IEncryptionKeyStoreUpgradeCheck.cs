// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// Defines a check of an encryption key store for keys stored under earlier names.
/// </summary>
public interface IEncryptionKeyStoreUpgradeCheck
{
    /// <summary>
    /// Inspects the key store.
    /// </summary>
    /// <param name="full">Whether to walk the whole key store. Otherwise only enough is read to tell whether earlier material is present.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="EncryptionKeyStoreUpgradeStatus"/>.</returns>
    Task<EncryptionKeyStoreUpgradeStatus> Inspect(bool full, CancellationToken cancellationToken = default);
}
