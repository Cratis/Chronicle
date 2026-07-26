// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

/// <summary>
/// Serializes a projection pipeline's read-modify-write cycle, either across the whole projection (coarse) or
/// striped per event source id, over a single shared, bounded set of stripes.
/// </summary>
/// <remarks>
/// What is being guarded is a shared read-model document in the sink, not grain memory:
/// <see cref="ProjectionPipeline.Handle"/> reads the current state, computes a changeset from it and writes the
/// result back, so two overlapping calls that resolve to the same document lose one another's writes. Nothing in the
/// actor model serializes that cycle, because <see cref="ProjectionPipeline"/> is a plain class cached once per
/// projection by the singleton <see cref="IProjectionPipelineManager"/> and shared by every observer subscriber that
/// dispatches into it. Striping per resolved key states the invariant precisely — handling that targets the same
/// document is serialized, handling that targets distinct documents runs in parallel.
/// <para>
/// Sharding observers into a grain per partition does not remove the need for this lock. Per-partition grain
/// isolation already exists: <see cref="Cratis.Chronicle.Concepts.Observation.ObserverSubscriberKey"/> carries the
/// partition's <see cref="EventSourceId"/>, so every partition already resolves to its own subscriber activation, on
/// the live delivery path and on the job path alike. The case a per-partition grain cannot serialize is a collapsing
/// projection — joins, a constant key, or parent hierarchy resolution map several partitions onto one document — and
/// that is exactly the case the coarse mode exists for.
/// </para>
/// <para>
/// The lock is process-local, since the manager that owns it is a per-process singleton, and the subscriber keying
/// is what makes that sufficient. A striped acquisition covers a single event source id, and the partition in
/// <see cref="Cratis.Chronicle.Concepts.Observation.ObserverSubscriberKey"/> gives that event source one subscriber
/// activation cluster wide, so two silos never handle it at the same time. A coarse acquisition covers a document
/// several event sources share, which no per-partition activation can serialize, so a collapsing projection
/// subscribes as <see cref="Cratis.Chronicle.Projections.ICollapsingProjectionObserverSubscriber"/> and receives
/// every partition through one activation - putting all of its handling in the process that owns this lock.
/// </para>
/// <para>
/// One instance is shared per projection (surviving pipeline cache eviction) so that concurrent handling across an
/// evicted-but-still-referenced pipeline and its replacement serialize on the same stripes. Growth is bounded to a
/// fixed number of <see cref="SemaphoreSlim"/> stripes regardless of event-source cardinality; distinct event source
/// ids that hash to the same stripe serialize unnecessarily, which reduces parallelism but never affects correctness.
/// </para>
/// <para>
/// A coarse acquisition holds every stripe, so it is mutually exclusive with any striped acquisition and with any
/// other coarse acquisition — matching the whole-projection serialization required by projections whose key
/// resolution can collapse distinct event sources onto one document. Because coarse and striped share the same
/// stripes, it is safe for two pipelines for the same projection to disagree on which mode to use.
/// </para>
/// </remarks>
public sealed class ProjectionHandleLock
{
    /// <summary>
    /// The number of stripes. A power of two so the stripe index is a cheap mask of the key hash.
    /// </summary>
    internal const int NumberOfStripes = 32;

    readonly SemaphoreSlim[] _stripes;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionHandleLock"/> class.
    /// </summary>
    public ProjectionHandleLock()
    {
        _stripes = new SemaphoreSlim[NumberOfStripes];
        for (var i = 0; i < NumberOfStripes; i++)
        {
            _stripes[i] = new SemaphoreSlim(1, 1);
        }
    }

    /// <summary>
    /// Acquire exclusive access across the whole projection, blocking every striped and coarse acquisition until
    /// released.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/> that releases the acquisition when disposed.</returns>
    public async Task<IDisposable> AcquireCoarse()
    {
        // Acquire the stripes in a fixed order; a striped acquisition only ever holds one stripe, so it can never
        // wait for a second stripe, which means acquiring them all in order cannot deadlock.
        for (var i = 0; i < _stripes.Length; i++)
        {
            await _stripes[i].WaitAsync();
        }

        return new Releaser(_stripes);
    }

    /// <summary>
    /// Acquire access for a single <see cref="EventSourceId"/>. Handling for the same event source id is serialized;
    /// handling for different event source ids may proceed in parallel.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to serialize handling for.</param>
    /// <returns>An <see cref="IDisposable"/> that releases the acquisition when disposed.</returns>
    public async Task<IDisposable> AcquireFor(EventSourceId eventSourceId)
    {
        var stripe = _stripes[StripeIndexFor(eventSourceId)];
        await stripe.WaitAsync();
        return new Releaser(stripe);
    }

    /// <summary>
    /// Gets the index of the stripe that serializes handling for a given <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to map to a stripe.</param>
    /// <returns>A stable, non-negative index in the range <c>[0, NumberOfStripes)</c> for the process lifetime.</returns>
    internal static int StripeIndexFor(EventSourceId eventSourceId) =>
        StringComparer.Ordinal.GetHashCode(eventSourceId.Value) & (NumberOfStripes - 1);

    sealed class Releaser(params SemaphoreSlim[] semaphores) : IDisposable
    {
        int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            foreach (var semaphore in semaphores)
            {
                semaphore.Release();
            }
        }
    }
}
