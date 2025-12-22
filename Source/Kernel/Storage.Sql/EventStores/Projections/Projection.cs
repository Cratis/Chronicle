// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.EntityFrameworkCore.Json;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Projections;

/// <summary>
/// Represents a projection.
/// </summary>
public class Projection
{
    /// <summary>
    /// Gets or sets the unique identifier for the projection.
    /// </summary>
    [Key]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the owner identifier for the projection.
    /// </summary>
    public ProjectionOwner Owner { get; set; }

    /// <summary>
    /// Gets or sets the name of the read model for the projection.
    /// </summary>
    public string ReadModelName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the generation of the read model for the projection.
    /// </summary>
    public uint ReadModelGeneration { get; set; }

    /// <summary>
    /// Gets or sets the type of the sink for the projection.
    /// </summary>
    public Guid SinkType { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets or sets the configuration identifier for the sink of the projection.
    /// </summary>
    public Guid SinkConfigurationId { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets or sets the versioned definitions associated with the projection.
    /// </summary>
    [Json]
    public IDictionary<uint, string> Definitions { get; set; } = new Dictionary<uint, string>();
}
