// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Observation;

namespace Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="ICanHandleReplayForObserver"/> for projections.
/// </summary>
public class ProjectionReplayHandler : ICanHandleReplayForObserver
{
    readonly IKernel _kernel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReducerReplayHandler"/> class.
    /// </summary>
    /// <param name="kernel"><see cref="IKernel"/> for accessing global artifacts.</param>
    public ProjectionReplayHandler(IKernel kernel)
    {
        _kernel = kernel;
    }

    /// <inheritdoc/>
    public Task<bool> CanHandle(ObserverDetails observerDetails) => Task.FromResult(observerDetails.Type == ObserverType.Projection);

    /// <inheritdoc/>
    public async Task BeginReplayFor(ObserverDetails observerDetails)
    {
        var @namespace = _kernel.GetEventStore((string)observerDetails.Key.MicroserviceId).GetNamespace(observerDetails.Key.TenantId);
        var pipeline = @namespace.ProjectionManager.GetPipeline(observerDetails.Identifier.Value);
        await pipeline.BeginReplay();
    }

    /// <inheritdoc/>
    public async Task EndReplayFor(ObserverDetails observerDetails)
    {
        var @namespace = _kernel.GetEventStore((string)observerDetails.Key.MicroserviceId).GetNamespace(observerDetails.Key.TenantId);
        var pipeline = @namespace.ProjectionManager.GetPipeline(observerDetails.Identifier.Value);
        await pipeline.EndReplay();
    }
}
