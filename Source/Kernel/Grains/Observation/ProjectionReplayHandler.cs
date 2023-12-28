// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Engines.Projections;
using Aksio.Cratis.Observation;
using Aksio.DependencyInversion;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="ICanHandleReplayForObserver"/> for projections.
/// </summary>
public class ProjectionReplayHandler : ICanHandleReplayForObserver
{
    readonly ProviderFor<IProjectionManager> _projectionManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReducerReplayHandler"/> class.
    /// </summary>
    /// <param name="projectionManager">Provider for <see cref="IProjectionManager"/>.</param>
    public ProjectionReplayHandler(
        ProviderFor<IProjectionManager> projectionManager)
    {
        _projectionManager = projectionManager;
    }

    /// <inheritdoc/>
    public Task<bool> CanHandle(ObserverDetails observerDetails) => Task.FromResult(observerDetails.Type == ObserverType.Projection);

    /// <inheritdoc/>
    public async Task BeginReplayFor(ObserverDetails observerDetails)
    {
        var pipeline = _projectionManager().GetPipeline(observerDetails.Identifier.Value);
        await pipeline.BeginReplay();
    }

    /// <inheritdoc/>
    public async Task EndReplayFor(ObserverDetails observerDetails)
    {
        var pipeline = _projectionManager().GetPipeline(observerDetails.Identifier.Value);
        await pipeline.EndReplay();
    }
}
