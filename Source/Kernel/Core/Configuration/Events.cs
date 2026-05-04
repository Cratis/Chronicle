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
    /// Bounded capacity of each appended-events queue channel.
    /// </summary>
    /// <remarks>
    /// When the channel is full the producer (AppendMany) awaits instead of returning, providing
    /// backpressure that prevents the kernel from accepting more appends than observers can process.
    /// A value of 0 means unbounded (no backpressure). Defaults to 2000 batches.
    /// </remarks>
    public int QueueBoundedCapacity { get; init; } = 2000;
}
