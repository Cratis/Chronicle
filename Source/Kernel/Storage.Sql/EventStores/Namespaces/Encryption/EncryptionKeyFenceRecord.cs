// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption;

/// <summary>Sealed request archives and silo protocol heartbeats, separate from key fences.</summary>
[PrimaryKey(nameof(Id))]
public class EncryptionKeyFenceRecord
{
    /// <summary>Gets or sets the opaque metadata identity.</summary>
    [MaxLength(256)]
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the serialized metadata.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets or sets the integrity digest.</summary>
    public string Seal { get; set; } = string.Empty;
}
