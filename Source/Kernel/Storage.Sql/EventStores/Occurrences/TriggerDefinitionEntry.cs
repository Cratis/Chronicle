// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;

/// <summary>
/// Stores an event-store trigger definition, keyed by the hash of its unbounded identity.
/// </summary>
public class TriggerDefinitionEntry
{
    /// <summary>
    /// Gets or sets the hashed identity.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original identity.
    /// </summary>
    public string Trigger { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON schema.
    /// </summary>
    public string Schema { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stored schema revision.
    /// </summary>
    public long Revision { get; set; }
}
