// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;

using KernelObservation = KernelCore::Cratis.Chronicle.Observation;
using KernelObserverConcepts = KernelConcepts::Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Keeps the benchmark's deliberately disconnected reactor from being restored by its live stream.
/// </summary>
internal sealed class CatchUpShapeSubscriptionControl : IIncomingGrainCallFilter, IDisposable
{
    /// <summary>
    /// The reactor whose subscription is controlled by the benchmark.
    /// </summary>
    public const string ReactorId = "catchup-shape-reactor";

    readonly SemaphoreSlim _gate = new(1, 1);
    bool _paused;

    /// <summary>
    /// Pauses restoration and waits for any in-progress restoration to finish before unsubscribing.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task Pause()
    {
        await _gate.WaitAsync();
        try
        {
            Volatile.Write(ref _paused, true);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Resumes normal subscription restoration.
    /// </summary>
    public void Resume() => Volatile.Write(ref _paused, false);

    /// <inheritdoc/>
    public void Dispose() => _gate.Dispose();

    /// <summary>
    /// Skips only the disconnected benchmark reactor's subscription keeper.
    /// </summary>
    /// <param name="context">The incoming grain call.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        if (!IsBenchmarkKeeper(context))
        {
            await context.Invoke();
            return;
        }

        await _gate.WaitAsync();
        try
        {
            if (!Volatile.Read(ref _paused)) await context.Invoke();
        }
        finally
        {
            _gate.Release();
        }
    }

    static bool IsBenchmarkKeeper(IIncomingGrainCallContext context)
    {
        // The keeper restores a lost client subscription through the observer; pausing that restore keeps the
        // benchmark's own unsubscribe in place.
        if (context.Grain is not KernelObservation.Observer) return false;
        if (context.MethodName != nameof(KernelObservation.IObserver.RestoreClientSubscription)) return false;
        return string.Equals(KernelObserverConcepts.ObserverKey.Parse(context.TargetId.Key.ToString()).ObserverId.Value, ReactorId, StringComparison.Ordinal);
    }
}
