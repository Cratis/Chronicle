// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Generations;

/// <summary>
/// Stores initialization audit evidence.
/// </summary>
public class GenerationInitializationBlockerRow
{
    /// <summary>
    /// Gets or sets Kind.
    /// </summary>
    public int Kind { get; set; }

    /// <summary>
    /// Gets or sets Subject.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets EventStore.
    /// </summary>
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets Detail.
    /// </summary>
    public string Detail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets RaisedAt.
    /// </summary>
    public DateTimeOffset RaisedAt { get; set; }

    /// <summary>
    /// Gets or sets ResolvedAt.
    /// </summary>
    public DateTimeOffset? ResolvedAt { get; set; }
}
