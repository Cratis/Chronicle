// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents configuration for jobs.
/// </summary>
public class Jobs
{
    /// <summary>
    /// Gets the maximum number of parallel job steps that can be executed concurrently.
    /// </summary>
    /// <remarks>
    /// If not configured, defaults to the number of processor threads minus 1, but never less than 1.
    /// </remarks>
    public int? MaxParallelSteps { get; init; }

    /// <summary>
    /// Gets the time threshold for considering a job as dead in the water.
    /// Jobs in preparation state with no steps that were created before this threshold are candidates for cleanup.
    /// Defaults to 1 hour.
    /// </summary>
    public TimeSpan DeadJobThreshold { get; init; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets the cleanup cadence for the scavenger process that removes dead jobs.
    /// This determines how often the cleanup process runs to check for and remove jobs stuck in preparation.
    /// Defaults to 1 hour.
    /// </summary>
    public TimeSpan CleanupCadence { get; init; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets the number of successfully handled events after which a replay/catch-up job step makes its
    /// progress checkpoint durable.
    /// </summary>
    /// <remarks>
    /// A partitioned or global-order replay step reports its progress after every consecutive batch it hands
    /// off, and each report persisted the whole step document. That write is debounced: the checkpoint is made
    /// durable once this many batches have been reported, and any other state write (a status change on
    /// completion, failure, or stop) flushes the pending checkpoint too, as does
    /// <see cref="StepCheckpointFlushInterval"/> for a step that has gone idle mid-range. A step always resumes
    /// from its last persisted checkpoint and re-reads forward, so a crash between debounced writes re-delivers
    /// every batch handled since that checkpoint. That redelivery bypasses the observer's cursor filter and is
    /// <em>not</em> harmless on its own: an accumulating projection or a reducer would fold the same events twice.
    /// It is made a no-op by <see cref="ReadModels.GuardSinkWritesOnWatermark"/>, which is only in force for read
    /// models keyed by event source id; a projection that collapses several event sources onto one document
    /// instead checkpoints after every batch. A larger value trades a longer post-crash re-scan for fewer writes.
    /// A replay step reports once per event cursor page rather than per batch, so for replay this counts pages, and
    /// the step also checkpoints at least every <see cref="ReplayCheckpointEventInterval"/> events. Live and
    /// catch-up behavior is unchanged.
    /// Defaults to 100.
    /// </remarks>
    public int StepCheckpointBatchInterval { get; init; } = 100;

    /// <summary>
    /// Gets how long a replay/catch-up job step may hold an unpersisted progress checkpoint before it is
    /// flushed regardless of how few batches have been reported.
    /// </summary>
    /// <remarks>
    /// <see cref="StepCheckpointBatchInterval"/> is a pure counter, so a step that hands off a few batches and
    /// then goes quiet — a slow subscriber, a sparse partition, a step that stops mid-range — would keep those
    /// batches unpersisted indefinitely and re-deliver all of them after a crash. This bounds that window in
    /// time. A replay step reports once per event cursor page, or every <see cref="ReplayCheckpointEventInterval"/>
    /// events when that comes first; this timer makes whatever has been reported durable. Live and catch-up
    /// behavior is unchanged.
    /// Defaults to 5 seconds; a value of zero or less disables the timed flush.
    /// </remarks>
    public TimeSpan StepCheckpointFlushInterval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets the number of handled events after which a replay job step makes its progress durable.
    /// </summary>
    /// <remarks>
    /// A replay step does not wait for the end of an event cursor page or for the flush timer: once it has handled
    /// this many events it reports them and checkpoints. The sink's write bulk then fills to its operation limit
    /// between checkpoint fan-outs, and after a crash a step re-reads at most about this many events beyond its
    /// durable checkpoint, which the sink's watermark guard turns into no-ops for models keyed by event source id.
    /// A value of zero or less disables the event-count trigger, leaving replay checkpoints at the end of each
    /// cursor page, debounced by <see cref="StepCheckpointBatchInterval"/> and <see cref="StepCheckpointFlushInterval"/>.
    /// Live and catch-up behavior is unchanged. Defaults to 10,000.
    /// </remarks>
    public int ReplayCheckpointEventInterval { get; init; } = 10_000;

    /// <summary>
    /// Gets the effective maximum parallel steps to use.
    /// </summary>
    /// <returns>The maximum parallel steps value.</returns>
    public int GetEffectiveMaxParallelSteps() => MaxParallelSteps ?? Math.Max(1, Environment.ProcessorCount - 1);
}
