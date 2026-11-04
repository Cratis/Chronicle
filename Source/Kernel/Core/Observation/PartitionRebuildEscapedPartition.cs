// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when a partition rebuild touches a key other than the partition it rebuilds.
/// </summary>
/// <param name="partition">The key of the partition being rebuilt.</param>
/// <param name="attempted">The key the rebuild attempted to touch.</param>
public class PartitionRebuildEscapedPartition(Key partition, Key attempted)
    : Exception($"A rebuild of partition '{partition}' attempted to touch partition '{attempted}'")
{
    /// <summary>
    /// Gets the key of the partition being rebuilt.
    /// </summary>
    public Key Partition { get; } = partition;

    /// <summary>
    /// Gets the key the rebuild attempted to touch.
    /// </summary>
    public Key Attempted { get; } = attempted;
}
