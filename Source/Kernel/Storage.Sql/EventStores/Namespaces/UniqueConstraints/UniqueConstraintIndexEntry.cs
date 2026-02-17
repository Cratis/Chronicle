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
}
