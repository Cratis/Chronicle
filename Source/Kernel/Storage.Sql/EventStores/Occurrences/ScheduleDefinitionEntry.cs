// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;

/// <summary>
/// A durable ScheduleDefinitionEntry row.
/// </summary>
public class ScheduleDefinitionEntry
{
    /// <summary>
    /// Gets or sets Id.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets Revision.
    /// </summary>
    public long Revision { get; set; }

    /// <summary>
    /// Gets or sets Data.
    /// </summary>
    public string Data { get; set; } = string.Empty;
}
