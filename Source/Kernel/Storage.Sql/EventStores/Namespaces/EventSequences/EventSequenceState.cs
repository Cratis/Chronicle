// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.EntityFrameworkCore.Json;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// Represents the entity for event sequence state entries in the database.
/// </summary>
public class EventSequenceState
{
    /// <summary>
    /// Gets or sets the event sequence identifier.
    /// </summary>
    [Key]
    public string EventSequenceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the next sequence number for the event sequence.
    /// </summary>
    public ulong SequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the tail sequence numbers per event type.
    /// </summary>
    [Json]
    public IDictionary<string, object> TailSequenceNumberPerEventType { get; set; } = new Dictionary<string, object>();
}
