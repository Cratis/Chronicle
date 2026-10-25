// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Projections.Generations;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Routes the bulk windows (replay and catch-up) of observers bound to a later generation to their generation
/// pipeline, holding the one writer grant captured when a window began for the whole window.
/// </summary>
/// <param name="generations">The <see cref="IGenerationProjectionResolver"/> that resolves and admits generations.</param>
/// <param name="projectionPipelineManager">The <see cref="IProjectionPipelineManager"/> that builds the pipelines.</param>
internal sealed class GenerationBulkWindows(
    IGenerationProjectionResolver generations,
    IProjectionPipelineManager projectionPipelineManager)
{
    readonly ConcurrentDictionary<ObserverKey, BulkWindow> _windows = new();

    /// <summary>
    /// Gets the generation pipeline for an observer. One bulk window runs on the one grant captured when it began,
    /// like the subscriber that holds its grant from activation: a reissued grant never moves the window to
    /// another pipeline or sink (fencing at write time stays authoritative) and a remembered grant that is no
    /// longer admitted fails closed.
    /// </summary>
    /// <param name="key">The observer key.</param>
    /// <param name="beginsWindow">True when this call begins (or resumes) a window and so captures the grant.</param>
    /// <returns>The pipeline, or null when the observer is not bound to a later generation.</returns>
    /// <exception cref="GenerationWriterGrantNotAdmitted">No grant is admitted for the generation.</exception>
    public async Task<IProjectionPipeline?> GetPipeline(ObserverKey key, bool beginsWindow)
    {
        if (!beginsWindow && _windows.TryGetValue(key, out var window))
        {
            await generations.Admit(key.EventStore, window.Execution);
            return await projectionPipelineManager.GetFor(key.EventStore, key.Namespace, window.Execution.Projection, window.Context);
        }

        var execution = await generations.Resolve(key.EventStore, key.Namespace, key.EventSequenceId, key.ObserverId);
        if (execution is null)
        {
            return null;
        }

        var context = await generations.Admit(key.EventStore, execution);
        if (beginsWindow)
        {
            _windows[key] = new(execution, context);
        }

        return await projectionPipelineManager.GetFor(key.EventStore, key.Namespace, execution.Projection, context);
    }

    /// <summary>
    /// Forgets the window of an observer.
    /// </summary>
    /// <param name="key">The observer key.</param>
    public void End(ObserverKey key) => _windows.TryRemove(key, out _);

    sealed record BulkWindow(GenerationExecution Execution, GenerationSinkContext Context);
}
