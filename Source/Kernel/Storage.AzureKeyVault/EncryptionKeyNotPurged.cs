// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure;

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// The exception that is thrown when an encryption key was deleted from Azure Key Vault but the vault refused to
/// purge it.
/// </summary>
/// <remarks>
/// A soft-deleted secret is recoverable by anyone allowed to recover secrets until the vault's retention period ends,
/// so a key that is only soft-deleted has not been erased. A vault with purge protection enabled cannot purge before
/// retention ends and therefore cannot support Chronicle's erasure; any other refusal means the identity Chronicle
/// runs as lacks permission to purge secrets. Repeating the deletion once the vault allows purging completes it.
/// </remarks>
/// <param name="secretName">The name of the secret that could not be purged.</param>
/// <param name="innerException">The <see cref="RequestFailedException"/> the vault answered with.</param>
public class EncryptionKeyNotPurged(string secretName, RequestFailedException innerException)
    : Exception(
        $"The encryption key secret '{secretName}' was deleted from Azure Key Vault, but the vault refused to purge it (HTTP {innerException.Status}). It stays recoverable until the vault's soft-delete retention period ends, so the erasure is not complete. A vault with purge protection enabled cannot support erasure; otherwise grant Chronicle's identity permission to purge secrets and repeat the deletion.",
        innerException);
