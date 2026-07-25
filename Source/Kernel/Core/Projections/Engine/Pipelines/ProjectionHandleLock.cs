// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

/// <summary>
/// Serializes a projection pipeline's read-modify-write cycle, either across the whole projection (coarse) or
/// striped per event source id, over a single shared, bounded set of stripes.
/// </summary>
/// <remarks>
/// A lock is required because <see cref="ProjectionPipeline"/> is a plain class, not a grain: its
/// <see cref="ProjectionPipeline.Handle"/> runs on the observer job steps off the grain activation thread, and the
/// pipeline is cached once per projection by the singleton <see cref="IProjectionPipelineManager"/> and shared by
/// every per-partition observer subscriber. Per-partition handling is therefore dispatched concurrently against the
/// one shared pipeline, outside any single grain's turn-based isolation, so Orleans does not serialize it. Striping
/// per resolved key preserves the actor-intended invariant — the same key is serialized, distinct keys run in
/// parallel — without a coarse whole-projection lock. The fully actor-native alternative (a grain per
/// (observer, partition) whose turn-based execution serializes each key with no explicit lock) is deliberately
/// deferred to the observer-sharding work package, which is design-first.
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
