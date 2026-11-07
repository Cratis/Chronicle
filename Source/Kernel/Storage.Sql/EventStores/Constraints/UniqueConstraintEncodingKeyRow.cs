// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Constraints;

/// <summary>
/// Represents the single row holding an event store's unique-constraint encoding key.
/// </summary>
/// <remarks>
/// This is deliberately a class and not a record, so a generated ToString can never print the material.
/// </remarks>
#pragma warning disable CA1819 // EF Core requires byte-array properties for fixed-length binary SQL columns.
public class UniqueConstraintEncodingKeyRow
{
    /// <summary>
    /// Gets or sets the singleton row identifier, always 1.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the 16 byte key identity.
    /// </summary>
    public byte[] KeyId { get; set; } = [];

    /// <summary>
    /// Gets or sets the 32 byte secret key material.
    /// </summary>
    public byte[] Material { get; set; } = [];
}
#pragma warning restore CA1819
