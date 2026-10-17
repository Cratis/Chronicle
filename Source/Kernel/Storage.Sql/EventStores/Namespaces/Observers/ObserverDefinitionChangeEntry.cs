// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers;

/// <summary>
/// An individually addressable definition-change row. The indexed metadata and JSON payload change in one statement.
/// </summary>
public class ObserverDefinitionChangeEntry
{
    /// <summary>
    /// Gets or sets the opaque complete row identity.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the opaque complete observer identity.
    /// </summary>
    public string Observer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the row family.
    /// </summary>
    public string Family { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the owning revision.
    /// </summary>
    public long Revision { get; set; }

    /// <summary>
    /// Gets or sets the compare-and-swap version.
    /// </summary>
    public long Version { get; set; }

    /// <summary>
    /// Gets or sets the forward-only state.
    /// </summary>
    public int State { get; set; }

    /// <summary>
    /// Gets or sets the optional durable position, without narrowing unsigned sequence numbers.
    /// </summary>
    public decimal? Position { get; set; }

    /// <summary>
    /// Gets or sets the complete row as native JSON.
    /// </summary>
    public string Data { get; set; } = string.Empty;
}
