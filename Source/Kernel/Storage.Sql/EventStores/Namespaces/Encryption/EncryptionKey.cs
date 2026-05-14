// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#pragma warning disable CA1819 // Allow arrays for properties

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption;

/// <summary>
/// Represents the entity for an encryption key.
/// </summary>
public class EncryptionKey
{
    /// <summary>
    /// Gets or sets the <see cref="Chronicle.Compliance.EncryptionKeyIdentifier"/> value the key is for.
    /// </summary>
    [Key]
    [Column(Order = 0)]
    [MaxLength(256)]
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the <see cref="Chronicle.Compliance.EncryptionKeyRevision"/> of the key.
    /// </summary>
    [Key]
    [Column(Order = 1)]
    public uint Revision { get; set; } = 1;

    /// <summary>
    /// Gets or sets the public key bytes.
    /// </summary>
    public byte[] PublicKey { get; set; } = [];

    /// <summary>
    /// Gets or sets the private key bytes.
    /// </summary>
    public byte[] PrivateKey { get; set; } = [];
}
