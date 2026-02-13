// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Projections;

/// <summary>
/// Represents a projection future in the database.
/// </summary>
public class ProjectionFutureEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the future.
    /// </summary>
    [Key]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the projection identifier.
    /// </summary>
    public string ProjectionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event sequence number.
    /// </summary>
    public ulong EventSequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the event type identifier.
    /// </summary>
    public string EventTypeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event type generation.
    /// </summary>
    public uint EventTypeGeneration { get; set; }

    /// <summary>
    /// Gets or sets the event source identifier.
    /// </summary>
    public string EventSourceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event content as JSON.
    /// </summary>
    public string EventContentJson { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent path.
    /// </summary>
    public string ParentPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the child path.
    /// </summary>
    public string ChildPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identified by property.
    /// </summary>
    public string IdentifiedByProperty { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent identified by property.
    /// </summary>
    public string ParentIdentifiedByProperty { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent key value as JSON.
    /// </summary>
    public string ParentKeyJson { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when this future was created.
    /// </summary>
    public DateTimeOffset Created { get; set; }
}
