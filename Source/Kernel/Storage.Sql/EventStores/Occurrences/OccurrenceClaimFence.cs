// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;

/// <summary>
/// A durable OccurrenceClaimFence row.
/// </summary>
public class OccurrenceClaimFence
{
    /// <summary>
    /// Gets or sets Id.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets Version.
    /// </summary>
    public long Version { get; set; }
}
