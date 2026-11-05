// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Vault;

/// <summary>
/// The secret directories an encryption key address is stored in.
/// </summary>
/// <param name="Directory">The directory revisions and the erasure are written to.</param>
/// <param name="LegacyDirectory">The directory used before segments were encoded, or null when the address does not use it.</param>
internal readonly record struct VaultKeyLocations(string Directory, string? LegacyDirectory)
{
    /// <summary>
    /// The leaf an erasure is stored under, beside the numbered revisions.
    /// </summary>
    /// <remarks>
    /// Revisions are numbered, so a non-numeric leaf can never collide with one, and the revision listing skips anything that does not parse as a number.
    /// </remarks>
    public const string ErasureLeaf = "erasure";

    /// <summary>
    /// Gets the path of the erasure secret.
    /// </summary>
    public string ErasurePath => $"{Directory}/{ErasureLeaf}";

    /// <summary>
    /// Gets the path of the earlier erasure secret, or null when the address does not use the earlier directory.
    /// </summary>
    public string? LegacyErasurePath => LegacyDirectory is null ? null : $"{LegacyDirectory}/{ErasureLeaf}";

    /// <summary>
    /// Gets the locations of an address.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/>.</param>
    /// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/>.</param>
    /// <param name="identifier">The <see cref="EncryptionKeyIdentifier"/>.</param>
    /// <returns>The <see cref="VaultKeyLocations"/>.</returns>
    public static VaultKeyLocations For(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        new(
            $"{VaultPathSegments.Encode(eventStore.Value, "event store")}/{VaultPathSegments.Encode(eventStoreNamespace.Value, "namespace")}/{VaultPathSegments.Encode(identifier.Value, "identifier")}",
            VaultPathSegments.LegacyDirectory(eventStore.Value, eventStoreNamespace.Value, identifier.Value));

    /// <summary>
    /// Gets the path of a revision's secret.
    /// </summary>
    /// <param name="revision">The <see cref="EncryptionKeyRevision"/>.</param>
    /// <returns>The secret path.</returns>
    public string Path(EncryptionKeyRevision revision) => $"{Directory}/{revision}";

    /// <summary>
    /// Gets the earlier path of a revision's secret.
    /// </summary>
    /// <param name="revision">The <see cref="EncryptionKeyRevision"/>.</param>
    /// <returns>The earlier secret path, or null when the address does not use the earlier directory.</returns>
    public string? LegacyPath(EncryptionKeyRevision revision) => LegacyDirectory is null ? null : $"{LegacyDirectory}/{revision}";
}
