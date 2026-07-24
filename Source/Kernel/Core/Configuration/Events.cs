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

    /// <summary>
    /// Number of appends after which an event sequence persists its state as a warm-start snapshot.
    /// </summary>
    /// <remarks>
    /// Event sequence state (the next sequence number and the per-event-type tails) is authoritative in the
    /// event tail and is rebuilt from it on activation, so it no longer needs to be written on every append.
    /// It is instead flushed every this many appends and on deactivation, purely to let a subsequent activation
    /// skip re-deriving the per-event-type tails. Correctness never depends on it: a crash between flushes loses
    /// no sequence-number correctness. Defaults to 1000 appends.
    /// </remarks>
    public int StatePersistenceInterval { get; init; } = 1000;
}
