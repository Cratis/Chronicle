// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Seeding;

/// <summary>
/// Represents the event seeding data in the database.
/// </summary>
public class EventSeedsEntity
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the events grouped by event type as JSON.
    /// </summary>
    public string ByEventTypeJson { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the events grouped by event source as JSON.
    /// </summary>
    public string ByEventSourceJson { get; set; } = string.Empty;
}
