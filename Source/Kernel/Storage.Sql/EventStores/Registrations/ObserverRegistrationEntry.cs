// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Registrations;

/// <summary>
/// Represents an independently versioned registration, owner setting, or generation floor row.
/// </summary>
public class ObserverRegistrationEntry
{
    /// <summary>
    /// Gets or sets the collision-resistant identity within the event store.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the record kind.
    /// </summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hashed program identifier for owner queries.
    /// </summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether a registration is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the fixed-width unsigned revision used by compare-and-set.
    /// </summary>
    public string Revision { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the fixed-width unsigned floor generation for ordered comparisons.
    /// </summary>
    public string Generation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the complete serialized record.
    /// </summary>
    public string Data { get; set; } = string.Empty;
}
