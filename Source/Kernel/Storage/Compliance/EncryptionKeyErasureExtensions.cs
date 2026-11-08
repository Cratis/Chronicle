// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// Extension methods for applying an <see cref="EncryptionKeyErasure"/> to a write.
/// </summary>
/// <remarks>
/// Every <see cref="IEncryptionKeyStorage"/> implementation has to make the same decisions - may this key be saved
/// at this revision, may a new one be provisioned at all, and what revision does it get - and getting any of them
/// wrong in one backend silently reopens the resurrection the fence exists to close. They live here so that all of
/// them decide once, in the same words.
/// </remarks>
public static class EncryptionKeyErasureExtensions
{
    /// <summary>Whether the fence refuses this exact revision and key material.</summary>
    /// <param name="erasure">The authoritative fence.</param>
    /// <param name="pair">The candidate revision and key.</param>
    /// <returns>Whether this candidate was erased.</returns>
    public static bool Refuses(this EncryptionKeyErasure? erasure, EncryptionKeyAtRevision pair) =>
        erasure is not null &&
        (pair.Revision.Value <= erasure.ErasedThrough.Value ||
         erasure.ErasedKeyFingerprints.Contains(pair.Key.Fingerprint, StringComparer.Ordinal));

    /// <summary>
    /// Determine whether a fence covers a stored or staged key.
    /// </summary>
    /// <param name="erasure">The fence, or null when no fence exists.</param>
    /// <param name="key">The key and its revision and optional staged lifecycle.</param>
    /// <returns>Whether the revision, material, or staged lifecycle is covered.</returns>
    public static bool Covers(this EncryptionKeyErasure? erasure, EncryptionKeyAtRevision key) =>
        erasure is not null &&
        (key.Revision.Value <= erasure.ErasedThrough.Value ||
         erasure.ErasedKeyFingerprints.Contains(key.Key.Fingerprint, StringComparer.Ordinal) ||
         (key.StagedLifecycle is { } lifecycle && lifecycle <= erasure.ErasedLifecycle));

    /// <summary>
    /// Validate a staged write against the current fence using the sweep and release predicate.
    /// </summary>
    /// <param name="erasure">The current, linearly read fence.</param>
    /// <param name="identifier">The key identifier.</param>
    /// <param name="key">The staged key carrying its observed lifecycle.</param>
    /// <exception cref="EncryptionKeyErased">The staged key is no longer allowed.</exception>
    public static void EnsureStillAllowed(this EncryptionKeyErasure erasure, EncryptionKeyIdentifier identifier, EncryptionKeyAtRevision key)
    {
        if (!erasure.NewKeyAllowed || erasure.Covers(key))
        {
            throw new EncryptionKeyErased(identifier, erasure);
        }
    }

    /// <summary>
    /// Merge a fence snapshot without weakening any boundary or losing a request.
    /// </summary>
    /// <param name="erasure">The current fence.</param>
    /// <param name="incoming">The snapshot to merge.</param>
    /// <returns>A closed, monotonic fence with an incremented version.</returns>
    public static EncryptionKeyErasure Merge(this EncryptionKeyErasure erasure, EncryptionKeyErasure incoming)
    {
        var requests = (erasure.Requests ?? []).Concat(incoming.Requests ?? [])
            .GroupBy(_ => _.RequestId, StringComparer.Ordinal)
            .Select(group => group.First() with
            {
                Fingerprints = Array.AsReadOnly(group.SelectMany(_ => _.Fingerprints ?? []).Distinct(StringComparer.Ordinal).ToArray()),
                Confirmed = group.Any(_ => _.Confirmed)
            })
            .ToArray();
        return erasure with
        {
            ErasedThrough = Math.Max(erasure.ErasedThrough.Value, incoming.ErasedThrough.Value),
            ErasedKeyFingerprints = Array.AsReadOnly(erasure.ErasedKeyFingerprints.Concat(incoming.ErasedKeyFingerprints).Distinct(StringComparer.Ordinal).ToArray()),
            NewKeyAllowed = false,
            Version = checked(erasure.Version + 1),
            Lifecycle = Math.Max(erasure.Lifecycle, incoming.Lifecycle),
            ErasedLifecycle = Math.Max(erasure.ErasedLifecycle, incoming.ErasedLifecycle),
            Requests = Array.AsReadOnly(requests)
        };
    }

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
