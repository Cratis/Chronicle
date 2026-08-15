// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// Defines a store for holding <see cref="EncryptionKey">encryption keys</see>.
/// </summary>
public interface IEncryptionKeyStorage
{
    /// <summary>
    /// Save an <see cref="EncryptionKey"/> for a specific <see cref="EncryptionKeyIdentifier"/>.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the key belongs to.</param>
    /// <param name="eventStoreNamespace"><see cref="EventStoreNamespaceName"/> the key belongs to.</param>
    /// <param name="identifier"><see cref="EncryptionKeyIdentifier"/> to save for.</param>
    /// <param name="key">The <see cref="EncryptionKey"/>.</param>
    /// <param name="revision">Optional <see cref="EncryptionKeyRevision"/>. Defaults to creating a new revision when not specified.</param>
    /// <returns>Async task.</returns>
    /// <exception cref="EncryptionKeyErased">Thrown when the write would undo a completed erasure.</exception>
    /// <remarks>
    /// A save at or below the <see cref="EncryptionKeyErasure.ErasedThrough"/> floor recorded for the identifier is
    /// refused, and so is a save of key material that was destroyed by an erasure — at any revision. Those two
    /// refusals are what stop a composed store healing a survivor back into a store that was erased, and a
    /// cross-event-store copy putting the original key material back where it was shredded.
    /// </remarks>
    Task SaveFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key, EncryptionKeyRevision? revision = null);

    /// <summary>
    /// Atomically get the existing <see cref="EncryptionKey"/> for an <see cref="EncryptionKeyIdentifier"/>,
    /// or persist and return the provided key when none exists yet.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the key belongs to.</param>
    /// <param name="eventStoreNamespace"><see cref="EventStoreNamespaceName"/> the key belongs to.</param>
    /// <param name="identifier"><see cref="EncryptionKeyIdentifier"/> to provision for.</param>
    /// <param name="key">The <see cref="EncryptionKey"/> to persist when none exists yet.</param>
    /// <returns>The existing key, or the provided key when it was the one persisted.</returns>
    /// <exception cref="EncryptionKeyErased">Thrown when the identifier was erased and no new lifecycle has been authorized.</exception>
    /// <remarks>
    /// <para>
    /// This is the get-or-create primitive for subject key provisioning. Concurrent or repeated callers for the
    /// same identifier — across a batch append, sibling projections, multiple silos, or a stale read that does not
    /// yet see a just-written key — must converge on a <b>single</b> persisted key pair, so that a value encrypted
    /// under the returned key can always be decrypted later. Unlike <see cref="SaveFor"/>, this never mints an
    /// additional revision when a key already exists; it reuses the initial revision. The default implementation is
    /// best-effort for stores without a native atomic insert-if-absent; stores that support one override this to be
    /// fully race-safe.
    /// </para>
    /// <para>
    /// It is also where a completed erasure is enforced. An identifier with an <see cref="EncryptionKeyErasure"/>
    /// recorded against it does not get a fresh key here — provisioning throws instead of quietly restarting
    /// protection for a person who asked to be forgotten. Once a new lifecycle has been authorized through
    /// <see cref="AllowNewKeyFor"/>, provisioning mints above the fence rather than back at the initial revision.
    /// </para>
    /// </remarks>
    async Task<EncryptionKey> GetOrAddFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key)
    {
        if (await TryGetFor(eventStore, eventStoreNamespace, identifier) is { } existing)
        {
            return existing;
        }

        var revision = (await GetErasureFor(eventStore, eventStoreNamespace, identifier)).RevisionForNewKey(identifier, key);
        await SaveFor(eventStore, eventStoreNamespace, identifier, key, revision);
        return await TryGetFor(eventStore, eventStoreNamespace, identifier) ?? key;
    }

    /// <summary>
    /// Check if there is an <see cref="EncryptionKey"/> for a specific <see cref="EncryptionKeyIdentifier"/>.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the key belongs to.</param>
    /// <param name="eventStoreNamespace"><see cref="EventStoreNamespaceName"/> the key belongs to.</param>
    /// <param name="identifier"><see cref="EncryptionKeyIdentifier"/> to check for.</param>
    /// <param name="revision">Optional <see cref="EncryptionKeyRevision"/>. Defaults to checking for the latest revision when not specified.</param>
    /// <returns>True if there is, false if not.</returns>
    Task<bool> HasFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null);

    /// <summary>
    /// Try to get an <see cref="EncryptionKey"/> for a specific <see cref="EncryptionKeyIdentifier"/>.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the key belongs to.</param>
    /// <param name="eventStoreNamespace"><see cref="EventStoreNamespaceName"/> the key belongs to.</param>
    /// <param name="identifier"><see cref="EncryptionKeyIdentifier"/> to get for.</param>
    /// <param name="revision">Optional <see cref="EncryptionKeyRevision"/>. Defaults to retrieving the latest revision when not specified.</param>
    /// <returns>The <see cref="EncryptionKey"/>, or <see langword="null"/> when none exists (e.g. it was deleted for right-to-erasure).</returns>
    Task<EncryptionKey?> TryGetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null);

    /// <summary>
    /// Get an <see cref="EncryptionKey"/> for a specific <see cref="EncryptionKeyIdentifier"/>.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the key belongs to.</param>
    /// <param name="eventStoreNamespace"><see cref="EventStoreNamespaceName"/> the key belongs to.</param>
    /// <param name="identifier"><see cref="EncryptionKeyIdentifier"/> to get for.</param>
    /// <param name="revision">Optional <see cref="EncryptionKeyRevision"/>. Defaults to retrieving the latest revision when not specified.</param>
    /// <returns>The <see cref="EncryptionKey"/>.</returns>
    /// <exception cref="MissingEncryptionKey">Thrown when no key exists for the identifier.</exception>
    Task<EncryptionKey> GetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null);

    /// <summary>
    /// Delete an <see cref="EncryptionKey"/> for a specific <see cref="EncryptionKeyIdentifier"/>.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the key belongs to.</param>
    /// <param name="eventStoreNamespace"><see cref="EventStoreNamespaceName"/> the key belongs to.</param>
    /// <param name="identifier"><see cref="EncryptionKeyIdentifier"/> to delete for.</param>
    /// <param name="revision">Optional <see cref="EncryptionKeyRevision"/>. Defaults to deleting all revisions when not specified.</param>
    /// <returns>Async task.</returns>
    /// <remarks>
    /// Removing key material is not by itself an erasure — it leaves the same absence as "never provisioned", which
    /// is what lets a later provisioning or a cross-event-store copy put a key back. An erasure is
    /// <see cref="RecordErasureFor"/> followed by this; that order matters, because the fence has to be in place
    /// before the material is gone.
    /// </remarks>
    Task DeleteFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null);

    /// <summary>
    /// Get the <see cref="EncryptionKeyErasure"/> recorded for a specific <see cref="EncryptionKeyIdentifier"/>.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the key belongs to.</param>
    /// <param name="eventStoreNamespace"><see cref="EventStoreNamespaceName"/> the key belongs to.</param>
    /// <param name="identifier"><see cref="EncryptionKeyIdentifier"/> to get the erasure for.</param>
    /// <returns>The <see cref="EncryptionKeyErasure"/>, or <see langword="null"/> when the identifier was never erased here.</returns>
    /// <remarks>
    /// This is the distinction a key store cannot otherwise make: <see langword="null"/> means never erased —
    /// whether or not a key exists — and a record means erased, whether or not anything was there to erase. The
    /// default implementation answers <see langword="null"/> because a store that cannot record an erasure has
    /// never recorded one; <see cref="RecordErasureFor"/> refuses loudly rather than letting it pretend otherwise.
    /// </remarks>
    Task<EncryptionKeyErasure?> GetErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        Task.FromResult<EncryptionKeyErasure?>(null);

    /// <summary>
    /// Record that an <see cref="EncryptionKeyIdentifier"/> has been erased, fencing off every revision and every
    /// piece of key material that exists for it right now.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the key belongs to.</param>
    /// <param name="eventStoreNamespace"><see cref="EventStoreNamespaceName"/> the key belongs to.</param>
    /// <param name="identifier"><see cref="EncryptionKeyIdentifier"/> to record the erasure for.</param>
    /// <returns>Async task.</returns>
    /// <exception cref="EncryptionKeyErasureNotSupported">Thrown when the store cannot record an erasure.</exception>
    /// <remarks>
    /// Call this <b>before</b> deleting the key material, not after: a fence written afterwards leaves a window in
    /// which the key is gone and nothing refuses a replacement, which is the exact window an erasure exists to
    /// close. Recording is idempotent and monotonic — the revision floor only rises and the fenced key material
    /// only accumulates, so recording twice, or recording over a fence a later lifecycle had opened, never weakens
    /// what is already fenced.
    /// </remarks>
    Task RecordErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        throw new EncryptionKeyErasureNotSupported(identifier);

    /// <summary>
    /// Authorize a later lawful lifecycle for an erased <see cref="EncryptionKeyIdentifier"/>, so that the next
    /// provisioning may mint a fresh key above the fence.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the key belongs to.</param>
    /// <param name="eventStoreNamespace"><see cref="EventStoreNamespaceName"/> the key belongs to.</param>
    /// <param name="identifier"><see cref="EncryptionKeyIdentifier"/> to authorize a new key for.</param>
    /// <returns>Async task.</returns>
    /// <exception cref="EncryptionKeyErasureNotSupported">Thrown when the store cannot record an erasure.</exception>
    /// <remarks>
    /// Erasure removes the key incarnation that exists now; it is not a permanent ban on the subject identifier.
    /// This is the deliberate, authorized step that lets the same person be protected again — it creates no key
    /// itself, and it never lifts the refusal of the destroyed key material, so the next key is a successor to the
    /// erased one and can decrypt nothing written before it. Calling it for an identifier that was never erased
    /// does nothing, because nothing is refusing.
    /// </remarks>
    Task AllowNewKeyFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        throw new EncryptionKeyErasureNotSupported(identifier);
}
