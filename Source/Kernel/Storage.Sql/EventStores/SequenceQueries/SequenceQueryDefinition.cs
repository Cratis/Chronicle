// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.EntityFrameworkCore.Json;
using Cratis.Chronicle.Concepts.SequenceQueries;

namespace Cratis.Chronicle.Storage.Sql.EventStores.SequenceQueries;

/// <summary>
/// Represents a saved event sequence query.
/// </summary>
public class SequenceQueryDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier of the query.
    /// </summary>
    [Key]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the display name the user gave the query.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets who the query is visible to.
    /// </summary>
    public SequenceQueryScope Scope { get; set; }

    /// <summary>
    /// Gets or sets the identity that saved the query.
    /// </summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the folder within the scope the query is filed under. Empty means it sits
    /// directly under its scope. Nested folders are separated by a forward slash.
    /// </summary>
    public string Folder { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace the query runs against.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event sequence the query runs against.
    /// </summary>
    public string EventSequenceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the narrowing the user configured.
    /// </summary>
    [Json]
    public SequenceQueryFilter Filter { get; set; } = new();

    /// <summary>
    /// Gets or sets what the results are ordered by.
    /// </summary>
    public SequenceQuerySortBy SortBy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether results are ordered from the highest value down.
    /// </summary>
    public bool Descending { get; set; }
}
