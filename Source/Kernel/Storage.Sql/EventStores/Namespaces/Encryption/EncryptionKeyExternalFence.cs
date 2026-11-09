// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption;

/// <summary>Isolated database authority for an external material store, not the database's own keys.</summary>
[PrimaryKey(nameof(Identifier))]
public class EncryptionKeyExternalFence
{
    /// <summary>Gets or sets the external store's scoped identifier.</summary>
    [MaxLength(256)]
    public string Identifier { get; set; } = string.Empty;

    /// <summary>Gets or sets the full unsigned CAS version.</summary>
    [Precision(20, 0)]
    public decimal Version { get; set; }

    /// <summary>Gets or sets the sealed state, publications and legacy markers.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Gets or sets the state integrity digest.</summary>
    public string Seal { get; set; } = string.Empty;
}
