// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;

/// <summary>
/// A durable ScheduleCursorEntry row.
/// </summary>
public class ScheduleCursorEntry
{
    /// <summary>
    /// Gets or sets Id.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets Token.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets LastMaterializedDue.
    /// </summary>
    public long LastMaterializedDue { get; set; }

    /// <summary>
    /// Gets or sets RegisteredRevision.
    /// </summary>
    public long RegisteredRevision { get; set; }

    /// <summary>
    /// Gets or sets AppliedRevision.
    /// </summary>
    public long AppliedRevision { get; set; }

    /// <summary>
    /// Gets or sets Version.
    /// </summary>
    public long Version { get; set; }
}
