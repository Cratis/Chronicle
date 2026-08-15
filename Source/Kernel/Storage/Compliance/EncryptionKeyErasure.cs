// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// Represents the record an <see cref="IEncryptionKeyStorage"/> keeps of a completed erasure for an
/// <see cref="EncryptionKeyIdentifier"/> - the fence that stops the erased key coming back.
/// </summary>
/// <remarks>
/// <para>
/// A key store that holds only key material has one absence and three meanings for it: never protected, erased,
/// and not provisioned yet. Every path that provisions on demand therefore reads a completed right-to-erasure as
/// an invitation to mint a fresh key, and every path that still holds the original material puts the original
/// back. This record is what makes <b>erased</b> a state the store can tell apart from <b>never provisioned</b>,
/// and it is kept beside the keys - not in memory, and not in a separate ledger - so that it is written in the
/// same breath as the deletion and travels with the store into its backups, its replicas and every composed
/// store that serves it.
/// </para>
/// <para>
/// The fence is not a ban on the subject identifier. A later, lawful lifecycle for the same person is supported
/// through <see cref="IEncryptionKeyStorage.AllowNewKeyFor"/>; it is simply never an accident.
/// </para>
/// </remarks>
/// <param name="ErasedThrough">The highest <see cref="EncryptionKeyRevision"/> the erasure covered. No key at or below it may be provisioned, saved or healed again.</param>
/// <param name="ErasedKeyFingerprints">Fingerprints of the key material that was destroyed. That material may never be stored again, at any revision.</param>
/// <param name="NewKeyAllowed">Whether an explicitly authorized new lifecycle may mint a fresh key above the fence.</param>
public record EncryptionKeyErasure(
    EncryptionKeyRevision ErasedThrough,
    IReadOnlyList<string> ErasedKeyFingerprints,
    bool NewKeyAllowed)
{
    /// <summary>
    /// Gets the <see cref="EncryptionKeyRevision"/> a new lifecycle mints its first key at.
    /// </summary>
    public EncryptionKeyRevision NextRevision => new(ErasedThrough.Value + 1u);

    /// <summary>
    /// Build the fence that covers an existing one plus the key material being destroyed now.
    /// </summary>
    /// <param name="existing">The <see cref="EncryptionKeyErasure"/> already recorded, or <see langword="null"/> when this is the first erasure.</param>
    /// <param name="present">The revisions and key material present in the store at the moment of the erasure.</param>
    /// <returns>The <see cref="EncryptionKeyErasure"/> to persist.</returns>
    /// <remarks>
    /// The floor only ever rises, and the fingerprints only ever accumulate, so recording an erasure twice - or
    /// recording one over a fence that a later lifecycle had opened - can never weaken what is already fenced.
    /// The floor is at least <see cref="EncryptionKeyRevision.Initial"/> even when no key was present, because a
    /// store that never held the subject's key must still refuse to mint one afterwards; that is the whole point
    /// of distinguishing an erasure from an absence.
    /// </remarks>
    public static EncryptionKeyErasure Covering(EncryptionKeyErasure? existing, IEnumerable<(EncryptionKeyRevision Revision, EncryptionKey Key)> present)
    {
        var materialized = present.ToArray();
        var highestPresent = materialized.Select(_ => _.Revision.Value).DefaultIfEmpty(0u).Max();
        var floor = Math.Max(Math.Max(highestPresent, existing?.ErasedThrough.Value ?? 0u), EncryptionKeyRevision.Initial.Value);
        var fingerprints = (existing?.ErasedKeyFingerprints ?? [])
            .Concat(materialized.Select(_ => _.Key.Fingerprint))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new(floor, fingerprints, NewKeyAllowed: false);
    }
}
