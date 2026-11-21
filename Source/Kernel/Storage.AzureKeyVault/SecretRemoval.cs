// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// The outcome of removing a secret.
/// </summary>
public enum SecretRemoval
{
    /// <summary>
    /// The secret was deleted and purged.
    /// </summary>
    Purged = 0,

    /// <summary>
    /// The secret was deleted, but the vault refused the purge, so it can still be recovered.
    /// </summary>
    DeletedNotPurged = 1
}
