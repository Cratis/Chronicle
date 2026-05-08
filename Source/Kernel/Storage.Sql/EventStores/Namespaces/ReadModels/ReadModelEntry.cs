// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Represents an entity for storing a read model document in a SQL table.
/// </summary>
public class ReadModelEntry
{
    /// <summary>
    /// Gets or sets the unique identifier for the read model instance — the primary key.
    /// </summary>
    [Key]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON-serialized document representing the full read model state.
    /// </summary>
    public string Document { get; set; } = string.Empty;
}
