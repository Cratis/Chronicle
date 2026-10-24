// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;

using KernelReactors = KernelCore::Cratis.Chronicle.Observation.Reactors.Clients;
using ReactorDefinition = KernelConcepts::Cratis.Chronicle.Concepts.Observation.Reactors.ReactorDefinition;

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
        if (context.Grain is not KernelReactors.Reactor) return false;
        if (context.MethodName != nameof(KernelReactors.IReactor.KeepSubscription)) return false;
        return context.Request.GetArgument(0) is ReactorDefinition { Identifier.Value: ReactorId };
    }
}
