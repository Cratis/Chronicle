// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Projections.Generations;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Routes the bulk windows (replay and catch-up) of observers bound to a later generation to their generation
/// pipeline, holding the one writer grant captured when a window began for the whole window.
/// </summary>
/// <param name="kind">The <see cref="GenerationBulkKind"/> of windows this instance serves.</param>
/// <param name="store">The silo-wide <see cref="GenerationBulkWindowStore"/> holding the open windows.</param>
/// <param name="generations">The <see cref="IGenerationProjectionResolver"/> that resolves and admits generations.</param>
/// <param name="projectionPipelineManager">The <see cref="IProjectionPipelineManager"/> that builds the pipelines.</param>
internal sealed class GenerationBulkWindows(
    GenerationBulkKind kind,
    GenerationBulkWindowStore store,
    IGenerationProjectionResolver generations,
    IProjectionPipelineManager projectionPipelineManager)
{
    /// <summary>
    /// Gets the generation pipeline for an observer. A begin always captures a new window, replacing a stale one.
    /// A resume reuses the open window (capturing one only when none exists, such as after a silo restart) and
    /// any other call reuses it too. A reused window is admission checked, so a revoked grant fails closed, and
    /// a reissued grant never moves the window to another pipeline or sink. Without a window nothing is stored
    /// outside begin and resume.
    /// </summary>
    /// <param name="key">The observer key.</param>
    /// <param name="call">What the call does to the window.</param>
    /// <returns>The pipeline, or null when the observer is not bound to a later generation.</returns>
    /// <exception cref="GenerationWriterGrantNotAdmitted">No grant is admitted for the generation.</exception>
    public async Task<IProjectionPipeline?> GetPipeline(ObserverKey key, GenerationBulkCall call)
    {
        if (call != GenerationBulkCall.Begin && store.TryGet(kind, key, out var window) && window is not null)
        {
            await generations.Admit(key.EventStore, window.Execution);
            return await projectionPipelineManager.GetFor(key.EventStore, key.Namespace, window.Execution.Projection, window.Context);
        }

        if (call == GenerationBulkCall.Begin)
        {
            store.Remove(kind, key);
        }

        var execution = await generations.Resolve(key.EventStore, key.Namespace, key.EventSequenceId, key.ObserverId);
        if (execution is null)
        {
            return null;
        }

        var context = await generations.Admit(key.EventStore, execution);
        if (call == GenerationBulkCall.Begin)
        {
            store.Set(kind, key, new(execution, context));
        }
        else if (call == GenerationBulkCall.Resume)
        {
            var resumed = store.GetOrAdd(kind, key, new(execution, context));
            await generations.Admit(key.EventStore, resumed.Execution);
            return await projectionPipelineManager.GetFor(key.EventStore, key.Namespace, resumed.Execution.Projection, resumed.Context);
        }

        return await projectionPipelineManager.GetFor(key.EventStore, key.Namespace, execution.Projection, context);
    }

    /// <summary>
    /// Forgets the window of an observer.
    /// </summary>
    /// <param name="key">The observer key.</param>
    public void End(ObserverKey key) => store.Remove(kind, key);
}
