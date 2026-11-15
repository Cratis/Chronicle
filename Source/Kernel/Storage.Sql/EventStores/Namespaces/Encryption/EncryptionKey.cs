// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CA1819 // Allow arrays for properties

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption;

/// <summary>
/// Represents the entity for an encryption key.
/// </summary>
[PrimaryKey(nameof(Identifier), nameof(Revision))]
public class EncryptionKey
{
    /// <summary>
    /// Gets or sets the <see cref="Chronicle.Compliance.EncryptionKeyIdentifier"/> value the key is for.
    /// </summary>
    [MaxLength(256)]
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the <see cref="Chronicle.Compliance.EncryptionKeyRevision"/> of the key.
    /// </summary>
    public uint Revision { get; set; } = 1;

    /// <summary>
    /// Gets or sets the public key bytes.
    /// </summary>
    public byte[] PublicKey { get; set; } = [];

    /// <summary>
    /// Gets or sets the private key bytes.
    /// </summary>
    public byte[] PrivateKey { get; set; } = [];

    /// <summary>
    /// Gets or sets the lifecycle that owns this row; null denotes a legacy row.
    /// </summary>
    public uint? StagedLifecycle { get; set; }

    /// <summary>
    /// Gets or sets the unique owner of an in-flight publication.
    /// </summary>
    public string? WriteNonce { get; set; }

    /// <summary>
    /// Gets or sets whether publication completed; null denotes a legacy row.
    /// </summary>
    public bool? Published { get; set; }
}
