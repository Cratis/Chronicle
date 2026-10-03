// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventSources;

/// <summary>
/// Represents the SQL storage entity for a registered event source definition.
/// </summary>
public class EventSourceDefinition
{
    /// <summary>
    /// Gets or sets the name of the event source, which is the identifier.
    /// </summary>
    [Key]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the description of the event source.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the owner of the definition.
    /// </summary>
    public int Owner { get; set; }

    /// <summary>
    /// Gets or sets the default concurrency dimensions.
    /// </summary>
    public int Concurrency { get; set; }

    /// <summary>
    /// Gets or sets the streams of the event source, serialized as JSON.
    /// </summary>
    public string Streams { get; set; } = "[]";
}
