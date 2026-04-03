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
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the read model.
    /// </summary>
    public ReadModelContainerName Name { get; set; } = ReadModelContainerName.NotSet;

    /// <summary>
    /// Gets or sets the owner identifier for the read model.
    /// </summary>
    public ReadModelOwner Owner { get; set; }

    /// <summary>
    /// Gets or sets the versioned schemas associated with the read model.
    /// </summary>
    [Json]
    public IDictionary<uint, string> Schemas { get; set; } = new Dictionary<uint, string>();
}
