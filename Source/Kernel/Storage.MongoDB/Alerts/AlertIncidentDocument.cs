// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Cratis.Chronicle.Storage.MongoDB.Alerts;

/// <summary>
/// Represents a flattened incident document.
/// </summary>
public class AlertIncidentDocument
{
    /// <summary>
    /// Gets or sets the Id column.
    /// </summary>
    [BsonId]
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the EventStore column.
    /// </summary>
    public string EventStore { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Namespace column.
    /// </summary>
    public string Namespace { get; set; } = null!;

    /// <summary>
    /// Gets or sets the ObserverId column.
    /// </summary>
    public string ObserverId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the EventSequenceId column.
    /// </summary>
    public string EventSequenceId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Partition column.
    /// </summary>
    public string Partition { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Condition column.
    /// </summary>
    public string Condition { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Severity column.
    /// </summary>
    public int? Severity { get; set; }

    /// <summary>
    /// Gets or sets the AttemptCount column.
    /// </summary>
    public int? AttemptCount { get; set; }

    /// <summary>
    /// Gets or sets the FirstFailure column.
    /// </summary>
    public string? FirstFailure { get; set; }

    /// <summary>
    /// Gets or sets the LastFailure column.
    /// </summary>
    public string? LastFailure { get; set; }

    /// <summary>
    /// Gets or sets the FailureKind column.
    /// </summary>
    public int? FailureKind { get; set; }

    /// <summary>
    /// Gets or sets the Message column.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the RaisedAt column.
    /// </summary>
    public string? RaisedAt { get; set; }

    /// <summary>
    /// Gets or sets the RaisedSequenceNumber column.
    /// </summary>
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal? RaisedSequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the LastChangedAt column.
    /// </summary>
    public string LastChangedAt { get; set; } = null!;

    /// <summary>
    /// Gets or sets the LastTransitionSequenceNumber column.
    /// </summary>
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal LastTransitionSequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the IsOpen column.
    /// </summary>
    public bool IsOpen { get; set; }

    /// <summary>
    /// Gets or sets the ClearedReason column.
    /// </summary>
    public int? ClearedReason { get; set; }
}
