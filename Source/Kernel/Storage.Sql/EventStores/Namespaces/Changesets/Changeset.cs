// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.EntityFrameworkCore.Json;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Changesets;

/// <summary>
/// Represents a changeset.
/// </summary>
public class Changeset
{
    /// <summary>
    /// Gets or sets the unique identifier for the changeset.
    /// </summary>
    [Key]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the read model name.
    /// </summary>
    public string ReadModelName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the read model key.
    /// </summary>
    public string ReadModelKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sequence number.
    /// </summary>
    public ulong SequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier.
    /// </summary>
    public Guid CorrelationId { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets or sets the changeset data as JSON.
    /// </summary>
    [Json]
    public string ChangesetData { get; set; } = string.Empty;
}
