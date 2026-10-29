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
    /// Gets or sets the exclusive position of settled constraint updates, or null before coverage was recorded.
    /// </summary>
    public ulong? ConstraintIndexWatermark { get; set; }

    /// <summary>
    /// Gets or sets the exclusive boundary of pending unique-constraint catch-up.
    /// </summary>
    public ulong? ConstraintIndexCatchUpBelow { get; set; }

    /// <summary>
    /// Gets or sets the JSON array of constraint names whose catch-up writes require append refusal.
    /// </summary>
    public string? ConstraintIndexCatchUpRefusing { get; set; }

    /// <summary>
    /// Gets or sets the mutation epoch advanced by every revision and redaction, or null before one was recorded.
    /// </summary>
    public ulong? MutationEpoch { get; set; }

    /// <summary>
    /// Gets or sets the tail sequence numbers per event type.
    /// </summary>
    [Json]
    public IDictionary<string, object> TailSequenceNumberPerEventType { get; set; } = new Dictionary<string, object>();
}
