// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.EntityFrameworkCore.Json;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.EventStores.ReadModels;

/// <summary>
/// Represents a read model.
/// </summary>
public class ReadModelDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier for the read model.
    /// </summary>
    [Key]
    public required ReadModelIdentifier Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the read model.
    /// </summary>
    public ReadModelContainerName Name { get; set; } = ReadModelContainerName.NotSet;

    /// <summary>
    /// Gets or sets the owner identifier for the read model.
    /// </summary>
    public ReadModelOwner Owner { get; set; }

    /// <summary>
    /// Gets or sets the source of the read model.
    /// </summary>
    public ReadModelSource Source { get; set; } = ReadModelSource.Unknown;

    /// <summary>
    /// Gets or sets the observer type (Projection vs Reducer) for the read model.
    /// </summary>
    public ReadModelObserverType ObserverType { get; set; } = ReadModelObserverType.NotSet;

    /// <summary>
    /// Gets or sets the identifier of the observer (projection or reducer) that owns the read model.
    /// </summary>
    public ReadModelObserverIdentifier ObserverIdentifier { get; set; } = ReadModelObserverIdentifier.Unspecified;

    /// <summary>
    /// Gets or sets the display name of the read model.
    /// </summary>
    public ReadModelDisplayName DisplayName { get; set; } = ReadModelDisplayName.NotSet;

    /// <summary>
    /// Gets or sets the type of the sink the read model is stored to.
    /// </summary>
    public string SinkType { get; set; } = Concepts.Sinks.SinkTypeId.None;

    /// <summary>
    /// Gets or sets the configuration identifier for the sink of the read model.
    /// </summary>
    public Guid SinkConfigurationId { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets or sets the versioned schemas associated with the read model.
    /// </summary>
    [Json]
    public IDictionary<uint, string> Schemas { get; set; } = new Dictionary<uint, string>();
}
