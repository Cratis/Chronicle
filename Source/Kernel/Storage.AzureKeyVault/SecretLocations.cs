// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// The secret name prefixes an encryption key address is stored under.
/// </summary>
/// <param name="Prefix">The prefix every current secret name of the address starts with.</param>
/// <param name="LegacyPrefix">The prefix of the names used before hashing, or null when the address does not use them.</param>
internal readonly record struct SecretLocations(string Prefix, string? LegacyPrefix)
{
    /// <summary>
    /// The suffix an erasure is stored under, beside the numbered revisions.
    /// </summary>
    /// <remarks>
    /// Revisions are numbered, so a non-numeric suffix can never collide with one, and the revision enumeration
    /// already skips anything that does not parse as a number - which is what keeps the fence out of it and lets
    /// a deletion of every revision leave the fence standing.
    /// </remarks>
    public const string ErasureSuffix = "erasure";

    /// <summary>
    /// Gets the name of the erasure secret.
    /// </summary>
    public string ErasureName => $"{Prefix}{ErasureSuffix}";

    /// <summary>
    /// Gets the name of the earlier erasure secret, or null when the address does not use earlier names.
    /// </summary>
    public string? LegacyErasureName => LegacyPrefix is null ? null : $"{LegacyPrefix}{ErasureSuffix}";

    /// <summary>
    /// Gets the locations of an address.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/>.</param>
    /// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/>.</param>
    /// <param name="identifier">The <see cref="EncryptionKeyIdentifier"/>.</param>
    /// <returns>The <see cref="SecretLocations"/>.</returns>
    public static SecretLocations For(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        new(
            SecretNames.KeyPrefix(eventStore, eventStoreNamespace, identifier),
            SecretNames.LegacyPrefix(eventStore, eventStoreNamespace, identifier));

    /// <summary>
    /// Gets the name of a revision's secret.
    /// </summary>
    /// <param name="revision">The <see cref="EncryptionKeyRevision"/>.</param>
    /// <returns>The secret name.</returns>
    public string Name(EncryptionKeyRevision revision) => $"{Prefix}{revision.Value}";

    /// <summary>
    /// Gets the earlier name of a revision's secret.
    /// </summary>
    /// <param name="revision">The <see cref="EncryptionKeyRevision"/>.</param>
    /// <returns>The earlier secret name, or null when the address does not use earlier names.</returns>
    public string? LegacyName(EncryptionKeyRevision revision) => LegacyPrefix is null ? null : $"{LegacyPrefix}{revision.Value}";
}
