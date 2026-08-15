// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption;

/// <summary>
/// Represents the entity for a recorded erasure of an encryption key.
/// </summary>
/// <remarks>
/// One row per erased identifier, in its own table beside the keys. A row here is the difference between "this
/// subject was erased" and "this subject was never protected here", which the key table alone cannot express.
/// </remarks>
[PrimaryKey(nameof(Identifier))]
public class EncryptionKeyErasure
{
    /// <summary>
    /// Gets or sets the <see cref="Chronicle.Compliance.EncryptionKeyIdentifier"/> value the erasure is for.
    /// </summary>
    [MaxLength(256)]
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the highest <see cref="Chronicle.Compliance.EncryptionKeyRevision"/> the erasure covered.
    /// </summary>
    public uint ErasedThrough { get; set; }

    /// <summary>
    /// Gets or sets the fingerprints of the destroyed key material, separated by commas.
    /// </summary>
    /// <remarks>
    /// Fingerprints are lowercase hexadecimal, so a comma can never occur inside one and needs no escaping.
    /// </remarks>
    public string ErasedKeyFingerprints { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether an authorized new lifecycle may mint a fresh key above the fence.
    /// </summary>
    public bool NewKeyAllowed { get; set; }
}
