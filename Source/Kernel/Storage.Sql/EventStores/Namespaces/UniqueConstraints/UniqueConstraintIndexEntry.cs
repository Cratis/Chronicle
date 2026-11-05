// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;

/// <summary>
/// Represents the SQL entry for a unique constraint index.
/// </summary>
public class UniqueConstraintIndexEntry
{
    /// <summary>
    /// Gets or sets the event source identifier.
    /// </summary>
    [Key]
    public string EventSourceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the constraint value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sequence number where the value exists.
    /// </summary>
    public decimal SequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the subject whose personal data the value derives from, or null when it holds none.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the stored hash of the owner's exact bytes, computed by SQL Server. It is absent on other providers.
    /// </summary>
#pragma warning disable CA1819 // EF Core requires byte-array properties for binary SQL columns.
    public byte[]? ExactKey { get; set; }
#pragma warning restore CA1819

    /// <summary>
    /// Gets or sets a value indicating whether the owner of the row is known exactly. Rows written by an earlier release are not.
    /// </summary>
    public bool ExactSpelling { get; set; }
}
