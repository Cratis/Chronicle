// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the events configuration.
/// </summary>
public class Events
{
    /// <summary>
    /// Number of appended event queues to use.
    /// </summary>
    /// <remarks>
    /// Each queue runs a dedicated async processing loop. Higher values allow
    /// more observer parallelism but consume more memory and idle CPU overhead.
    /// 2 queues is sufficient for most installations; increase for high-throughput
    /// scenarios with many concurrent observers.
    /// </remarks>
    public int Queues { get; init; } = 2;

    /// <summary>
    /// Maximum number of observer dispatches that run concurrently within a single queue's <c>HandlePartitioned</c> loop.
    /// </summary>
    /// <remarks>
    /// Bounding this prevents unbounded <c>Task.WhenAll</c> fan-out from exhausting the MongoDB connection pool
    /// when many observers are registered. Defaults to the processor count (minimum 2).
    /// Increase for systems with many observers or reduce to lower MongoDB connection pressure.
    /// </remarks>
    public int MaxConcurrentObserverDispatches { get; init; } = Math.Max(2, Environment.ProcessorCount);

    /// <summary>
    /// Maximum number of events validated concurrently within a single <c>AppendMany</c> call.
    /// </summary>
    /// <remarks>
    /// Each concurrent validation can issue MongoDB reads for compliance and constraint checking.
    /// Capping this prevents a large batch from spawning an unbounded number of concurrent
    /// database round-trips. Defaults to twice the processor count (minimum 4).
    /// </remarks>
    public int MaxConcurrentEventValidations { get; init; } = Math.Max(4, Environment.ProcessorCount * 2);

    /// <summary>
    /// Bounded capacity of each appended-events queue channel.
    /// </summary>
    /// <remarks>
    /// When the channel is full the producer (AppendMany) awaits instead of returning, providing
    /// backpressure that prevents the kernel from accepting more appends than observers can process.
    /// A value of 0 means unbounded (no backpressure). Defaults to 2000 batches.
    /// </remarks>
    public int QueueBoundedCapacity { get; init; } = 2000;
}
