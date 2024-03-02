// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance;

namespace Aksio.Cratis.Kernel.Storage.Compliance;

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
    /// <returns>Async task.</returns>
    Task SaveFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key);

    /// <summary>
    /// Check if there is an <see cref="EncryptionKey"/> for a specific <see cref="EncryptionKeyIdentifier"/>.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the key belongs to.</param>
    /// <param name="eventStoreNamespace"><see cref="EventStoreNamespaceName"/> the key belongs to.</param>
    /// <param name="identifier"><see cref="EncryptionKeyIdentifier"/> to check for.</param>
    /// <returns>True if there is, false if not.</returns>
    Task<bool> HasFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier);

    /// <summary>
    /// Get an <see cref="EncryptionKey"/> for a specific <see cref="EncryptionKeyIdentifier"/>.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the key belongs to.</param>
    /// <param name="eventStoreNamespace"><see cref="EventStoreNamespaceName"/> the key belongs to.</param>
    /// <param name="identifier"><see cref="EncryptionKeyIdentifier"/> to get for.</param>
    /// <returns>The <see cref="EncryptionKey"/>.</returns>
    Task<EncryptionKey> GetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier);

    /// <summary>
    /// Delete an <see cref="EncryptionKey"/> for a specific <see cref="EncryptionKeyIdentifier"/>.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the key belongs to.</param>
    /// <param name="eventStoreNamespace"><see cref="EventStoreNamespaceName"/> the key belongs to.</param>
    /// <param name="identifier"><see cref="EncryptionKeyIdentifier"/> to delete for.</param>
    /// <returns>Async task.</returns>
    Task DeleteFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier);
}
