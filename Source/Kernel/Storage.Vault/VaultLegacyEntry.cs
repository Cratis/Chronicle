// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault;

/// <summary>
/// Represents a secret stored by an earlier version, outside <see cref="VaultPathSegments.Root"/>.
/// </summary>
/// <param name="Path">The path of the secret.</param>
/// <param name="Destination">The path the secret has under <see cref="VaultPathSegments.Root"/>, or null when it cannot be tied to one address.</param>
/// <param name="Revision">The revision of the key, or null when the secret is an erasure record or cannot be tied to one address.</param>
internal record VaultLegacyEntry(string Path, string? Destination, uint? Revision)
{
    /// <summary>
    /// Gets whether the secret cannot be tied to one address, so it is left for review.
    /// </summary>
    public bool IsAmbiguous => Destination is null;

    /// <summary>
    /// Gets whether the secret is an erasure record of an address.
    /// </summary>
    public bool IsErasure => Destination is not null && Revision is null;

    /// <summary>
    /// Gets the directory of the address under <see cref="VaultPathSegments.Root"/>, or null when there is none.
    /// </summary>
    public string? Directory => Destination?[..Destination.LastIndexOf('/')];

    /// <summary>
    /// Classifies the segments of a secret path from an earlier version.
    /// </summary>
    /// <param name="segments">The segments of the path.</param>
    /// <returns>The <see cref="VaultLegacyEntry"/>.</returns>
    /// <remarks>
    /// Only a path of exactly an event store, a namespace, an identifier and a leaf can be tied to one address, and only
    /// when <see cref="VaultPathSegments.LegacyDirectory"/> accepts its directory. The leaf must be
    /// <see cref="VaultKeyLocations.ErasureLeaf"/> or a revision written the way this version writes it. Anything else is
    /// ambiguous.
    /// </remarks>
    public static VaultLegacyEntry From(IReadOnlyList<string> segments)
    {
        var path = string.Join('/', segments);
        if (segments.Count != 4 || VaultPathSegments.LegacyDirectory(segments[0], segments[1], segments[2]) is not { } legacyDirectory)
        {
            return new(path, null, null);
        }

        VaultKeyLocations locations;
        try
        {
            locations = VaultKeyLocations.For(segments[0], segments[1], segments[2]);
        }
        catch (InvalidEncryptionKeyAddress)
        {
            return new(path, null, null);
        }

        if (locations.LegacyDirectory != legacyDirectory)
        {
            return new(path, null, null);
        }

        var leaf = segments[3];
        if (leaf == VaultKeyLocations.ErasureLeaf)
        {
            return new(path, locations.ErasurePath, null);
        }

        return uint.TryParse(leaf, NumberStyles.None, CultureInfo.InvariantCulture, out var revision) &&
            revision.ToString(CultureInfo.InvariantCulture) == leaf
            ? new(path, locations.Path(revision), revision)
            : new(path, null, null);
    }
}
