// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Sql.Json;

namespace Cratis.Chronicle.Storage.Sql.ReadModels;

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
    /// Gets or sets the owner identifier for the read model.
    /// </summary>
    public ReadModelOwner Owner { get; set; }

    /// <summary>
    /// Gets or sets the versioned schemas associated with the read model.
    /// </summary>
    [Json]
    public IDictionary<uint, string> Schemas { get; set; } = new Dictionary<uint, string>();
}
