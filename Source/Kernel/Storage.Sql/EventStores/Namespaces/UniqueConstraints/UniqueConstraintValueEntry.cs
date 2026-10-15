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
}
