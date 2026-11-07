// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CA1819 // Allow arrays for properties

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption;

/// <summary>
/// Represents the entity for a kernel-internal secret.
/// </summary>
/// <remarks>
/// Internal secrets have their own table, which no subject key operation reads or writes. They are per event store and
/// are kept in the default namespace's database.
/// </remarks>
[PrimaryKey(nameof(Purpose))]
public class InternalSecret
{
    /// <summary>
    /// Gets or sets the <see cref="Cratis.Chronicle.Storage.Compliance.InternalSecretPurpose"/> value the secret is for.
    /// </summary>
    [MaxLength(64)]
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the public part of the secret.
    /// </summary>
    public byte[] PublicKey { get; set; } = [];

    /// <summary>
    /// Gets or sets the private part of the secret.
    /// </summary>
    public byte[] PrivateKey { get; set; } = [];
}
