// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams;

/// <summary>
/// Represents a SQL entity for a closed stream entry.
/// </summary>
public class ClosedStreamEntry
{
    /// <summary>
    /// Gets or sets the event sequence identifier.
    /// </summary>
    [Key]
    [MaxLength(255)]
    public string EventSequenceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stream type.
    /// </summary>
    [Key]
    [MaxLength(255)]
    public string StreamType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stream identifier.
    /// </summary>
    [Key]
    [MaxLength(255)]
    public string StreamId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event source identifier, or empty when unset.
    /// </summary>
    [MaxLength(255)]
    public string EventSourceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event source type, or empty when unset.
    /// </summary>
    [MaxLength(255)]
    public string EventSourceType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the closure owner, or empty for manual closures.
    /// </summary>
    [MaxLength(255)]
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the participating dimensions.
    /// </summary>
    public int Dimensions { get; set; }

    /// <summary>
    /// Gets or sets the closing sequence number.
    /// </summary>
    public decimal SequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the closing timestamp.
    /// </summary>
    public DateTimeOffset? ClosedAt { get; set; }
}
