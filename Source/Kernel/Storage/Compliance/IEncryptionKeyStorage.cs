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
    /// <remarks>
    /// This is the get-or-create primitive for subject key provisioning. Concurrent or repeated callers for the
    /// same identifier — across a batch append, sibling projections, multiple silos, or a stale read that does not
    /// yet see a just-written key — must converge on a <b>single</b> persisted key pair, so that a value encrypted
    /// under the returned key can always be decrypted later. Unlike <see cref="SaveFor"/>, this never mints an
    /// additional revision when a key already exists; it reuses the initial revision. The default implementation is
    /// best-effort for stores without a native atomic insert-if-absent; stores that support one override this to be
    /// fully race-safe.
    /// </remarks>
    async Task<EncryptionKey> GetOrAddFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key)
    {
        if (await TryGetFor(eventStore, eventStoreNamespace, identifier) is { } existing)
        {
            return existing;
        }

        await SaveFor(eventStore, eventStoreNamespace, identifier, key, EncryptionKeyRevision.Initial);
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
    Task DeleteFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null);
}
