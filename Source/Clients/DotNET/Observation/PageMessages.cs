// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reducers;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Extension methods for messages that deliver events either for one partition or as a page of partitions.
/// </summary>
internal static class PageMessages
{
    /// <summary>
    /// Gets the partition a delivery is scheduled under, so deliveries for the same partition run in order.
    /// </summary>
    /// <param name="events">The <see cref="EventsToObserve"/> delivered.</param>
    /// <returns>The partition, or null when the delivery spans several partitions and must run on its own.</returns>
    public static string? SchedulingPartition(this EventsToObserve events) =>
        events.Partitions.Count switch
        {
            0 => events.Partition ?? string.Empty,
            1 => events.Partitions[0].Partition ?? string.Empty,
            _ => null
        };

    /// <summary>
    /// Gets the partition a delivery is scheduled under, so deliveries for the same partition run in order.
    /// </summary>
    /// <param name="operation">The <see cref="ReduceOperationMessage"/> delivered.</param>
    /// <returns>The partition, or null when the delivery spans several partitions and must run on its own.</returns>
    public static string? SchedulingPartition(this ReduceOperationMessage operation) =>
        operation.Partitions.Count switch
        {
            0 => operation.Partition ?? string.Empty,
            1 => operation.Partitions[0].Partition ?? string.Empty,
            _ => null
        };

    /// <summary>
    /// Gets the partitions of a delivery, treating a single-partition delivery as a page of one.
    /// </summary>
    /// <param name="events">The <see cref="EventsToObserve"/> delivered.</param>
    /// <returns>The <see cref="PartitionEvents"/> of the delivery.</returns>
    public static IReadOnlyList<PartitionEvents> PartitionsOf(this EventsToObserve events) =>
        events.Partitions.Count > 0
            ? [.. events.Partitions]
            : [new PartitionEvents { Partition = events.Partition ?? string.Empty, Events = events.Events }];

    /// <summary>
    /// Gets the partitions of a delivery, treating a single-partition delivery as a page of one.
    /// </summary>
    /// <param name="operation">The <see cref="ReduceOperationMessage"/> delivered.</param>
    /// <returns>The <see cref="ReducerPartition"/> of the delivery.</returns>
    public static IReadOnlyList<ReducerPartition> PartitionsOf(this ReduceOperationMessage operation) =>
        operation.Partitions.Count > 0
            ? [.. operation.Partitions]
            : [new ReducerPartition { Partition = operation.Partition ?? string.Empty, InitialState = operation.InitialState, InitialStateUnchanged = operation.InitialStateUnchanged, Events = operation.Events }];
}
