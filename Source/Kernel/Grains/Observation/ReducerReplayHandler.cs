// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Observation.Reducers;
using Aksio.Cratis.Observation;
using Aksio.DependencyInversion;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="ICanHandleReplayForObserver"/> for reducers.
/// </summary>
public class ReducerReplayHandler : ICanHandleReplayForObserver
{
    readonly ProviderFor<IReducerPipelineDefinitions> _pipelineDefinitions;
    readonly ProviderFor<IReducerPipelines> _pipelines;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReducerReplayHandler"/> class.
    /// </summary>
    /// <param name="pipelineDefinitions">Provider for <see cref="IReducerPipelineDefinitions"/>.</param>
    /// <param name="pipelines">Provider for <see cref="IReducerPipelines"/>.</param>
    public ReducerReplayHandler(
        ProviderFor<IReducerPipelineDefinitions> pipelineDefinitions,
        ProviderFor<IReducerPipelines> pipelines)
    {
        _pipelineDefinitions = pipelineDefinitions;
        _pipelines = pipelines;
    }

    /// <inheritdoc/>
    public Task<bool> CanHandle(ObserverDetails observerDetails) => Task.FromResult(observerDetails.Type == ObserverType.Reducer);

    /// <inheritdoc/>
    public async Task BeginReplayFor(ObserverDetails observerDetails)
    {
        if (await _pipelineDefinitions().HasFor(observerDetails.Identifier))
        {
            var definition = await _pipelineDefinitions().GetFor(observerDetails.Identifier);
            var pipeline = await _pipelines().GetFor(definition);
            await pipeline.BeginReplay();
        }
    }

    /// <inheritdoc/>
    public async Task EndReplayFor(ObserverDetails observerDetails)
    {
        if (await _pipelineDefinitions().HasFor(observerDetails.Identifier))
        {
            var definition = await _pipelineDefinitions().GetFor(observerDetails.Identifier);
            var pipeline = await _pipelines().GetFor(definition);
            await pipeline.EndReplay();
        }
    }
}
