// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// The exception that is thrown when storing an <see cref="EncryptionKey"/> would undo a completed erasure.
/// </summary>
/// <remarks>
/// Crypto-shredding is only an erasure while the key stays destroyed, so the store refuses rather than quietly
/// minting a replacement or quietly accepting the old material back. Reaching this means one of three things:
/// personal data is being written for a subject whose key was erased, a composed store or a cross-event-store
/// subscription is trying to heal the erased key back in, or a lawful new lifecycle for the subject has simply
/// not been authorized yet - see <see cref="IEncryptionKeyStorage.AllowNewKeyFor"/> for that last one.
/// </remarks>
/// <param name="identifier">The <see cref="EncryptionKeyIdentifier"/> that was erased.</param>
/// <param name="erasure">The <see cref="EncryptionKeyErasure"/> that refused the operation.</param>
public class EncryptionKeyErased(EncryptionKeyIdentifier identifier, EncryptionKeyErasure erasure)
    : Exception(
        $"The encryption key for identifier '{identifier}' was erased through revision {erasure.ErasedThrough} and cannot be provisioned, saved or restored again. Authorize a new key for the subject if a later lawful lifecycle needs one; the erased key itself never comes back.")
{
    /// <summary>
    /// Gets the <see cref="EncryptionKeyErasure"/> that refused the operation.
    /// </summary>
    public EncryptionKeyErasure Erasure { get; } = erasure;
}
