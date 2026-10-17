// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;

/// <summary>
/// Stores the fixed lifecycle snapshot and its acknowledged namespace prefix.
/// </summary>
public class LifecycleFanOutEntry
{
    /// <summary>
    /// Gets or sets the lifecycle identity.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the immutable serialized intent.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace count.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets the acknowledged namespace prefix.
    /// </summary>
    public int Progress { get; set; }
}
