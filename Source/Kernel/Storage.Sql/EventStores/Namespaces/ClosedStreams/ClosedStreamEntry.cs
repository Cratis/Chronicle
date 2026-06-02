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
}
