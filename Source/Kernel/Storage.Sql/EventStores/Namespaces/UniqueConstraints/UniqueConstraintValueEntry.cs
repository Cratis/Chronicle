// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;

/// <summary>
/// Represents a retained unique value and its owning event source.
/// </summary>
public class UniqueConstraintValueEntry
{
    /// <summary>
    /// Gets or sets the hashed value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event source owning the value.
    /// </summary>
    public string EventSourceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sequence number of the claim.
    /// </summary>
    public decimal SequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the subject whose personal data the value derives from, or null when it holds none.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the owner of the row is known exactly. Rows written by an earlier release are not.
    /// </summary>
    public bool ExactSpelling { get; set; }
}
