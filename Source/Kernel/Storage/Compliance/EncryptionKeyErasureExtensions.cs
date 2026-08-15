// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// Extension methods for applying an <see cref="EncryptionKeyErasure"/> to a write.
/// </summary>
/// <remarks>
/// Every <see cref="IEncryptionKeyStorage"/> implementation has to make the same two decisions - may this key be
/// saved at this revision, and what revision does a newly provisioned key get - and getting either of them wrong
/// in one backend silently reopens the resurrection the fence exists to close. They live here so that all of them
/// make the decision once, in the same words.
/// </remarks>
public static class EncryptionKeyErasureExtensions
{
    /// <summary>
    /// Ensure a key may be written at a revision.
    /// </summary>
    /// <param name="erasure">The <see cref="EncryptionKeyErasure"/> recorded for the identifier, or <see langword="null"/> when it was never erased.</param>
    /// <param name="identifier">The <see cref="EncryptionKeyIdentifier"/> being written for.</param>
    /// <param name="revision">The <see cref="EncryptionKeyRevision"/> the key would be written at.</param>
    /// <param name="key">The <see cref="EncryptionKey"/> being written.</param>
    /// <exception cref="EncryptionKeyErased">Thrown when the write would undo a completed erasure.</exception>
    public static void EnsureCanSave(this EncryptionKeyErasure? erasure, EncryptionKeyIdentifier identifier, EncryptionKeyRevision revision, EncryptionKey key)
    {
        if (erasure is null)
        {
            return;
        }

        // Two independent refusals. The revision floor stops a store being healed back to the incarnation that was
        // erased. The fingerprint stops the exact destroyed material returning at any revision at all - which is
        // what a cross-event-store copy carries, and the only thing that can make already-shredded ciphertext
        // readable again. The fingerprint refusal deliberately outlives an authorized new lifecycle.
        if (revision.Value <= erasure.ErasedThrough.Value || erasure.ErasedKeyFingerprints.Contains(key.Fingerprint, StringComparer.Ordinal))
        {
            throw new EncryptionKeyErased(identifier, erasure);
        }
    }

    /// <summary>
    /// Ensure a new key may be provisioned for an identifier.
    /// </summary>
    /// <param name="erasure">The <see cref="EncryptionKeyErasure"/> recorded for the identifier, or <see langword="null"/> when it was never erased.</param>
    /// <param name="identifier">The <see cref="EncryptionKeyIdentifier"/> being provisioned for.</param>
    /// <param name="key">The <see cref="EncryptionKey"/> that would be provisioned.</param>
    /// <exception cref="EncryptionKeyErased">Thrown when provisioning would undo a completed erasure.</exception>
    public static void EnsureCanProvision(this EncryptionKeyErasure? erasure, EncryptionKeyIdentifier identifier, EncryptionKey key)
    {
        if (erasure is null)
        {
            return;
        }

        if (!erasure.NewKeyAllowed || erasure.ErasedKeyFingerprints.Contains(key.Fingerprint, StringComparer.Ordinal))
        {
            throw new EncryptionKeyErased(identifier, erasure);
        }
    }

    /// <summary>
    /// Get the <see cref="EncryptionKeyRevision"/> a newly provisioned key is created at.
    /// </summary>
    /// <param name="erasure">The <see cref="EncryptionKeyErasure"/> recorded for the identifier, or <see langword="null"/> when it was never erased.</param>
    /// <param name="identifier">The <see cref="EncryptionKeyIdentifier"/> being provisioned for.</param>
    /// <param name="key">The <see cref="EncryptionKey"/> that would be provisioned.</param>
    /// <returns>The <see cref="EncryptionKeyRevision"/> to provision at.</returns>
    /// <exception cref="EncryptionKeyErased">Thrown when provisioning would undo a completed erasure.</exception>
    /// <remarks>
    /// A subject that was never erased is provisioned at <see cref="EncryptionKeyRevision.Initial"/>, exactly as
    /// before. A subject that was erased is refused outright unless a new lifecycle has been authorized, and then
    /// starts above the fence rather than back at the beginning - so the new key is a successor to the erased one
    /// rather than a replacement of it.
    /// </remarks>
    public static EncryptionKeyRevision RevisionForNewKey(this EncryptionKeyErasure? erasure, EncryptionKeyIdentifier identifier, EncryptionKey key)
    {
        erasure.EnsureCanProvision(identifier, key);
        return erasure is null ? EncryptionKeyRevision.Initial : erasure.NextRevision;
    }
}
