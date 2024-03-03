// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Observation;

namespace Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="ICanHandleReplayForObserver"/> for projections.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReducerReplayHandler"/> class.
/// </remarks>
/// <param name="kernel"><see cref="IKernel"/> for accessing global artifacts.</param>
public class ProjectionReplayHandler(IKernel kernel) : ICanHandleReplayForObserver
{
    /// <inheritdoc/>
    public Task<bool> CanHandle(ObserverDetails observerDetails) => Task.FromResult(observerDetails.Type == ObserverType.Projection);

    /// <inheritdoc/>
    public async Task BeginReplayFor(ObserverDetails observerDetails)
    {
        var @namespace = kernel.GetEventStore((string)observerDetails.Key.MicroserviceId).GetNamespace(observerDetails.Key.TenantId);
        var pipeline = @namespace.ProjectionManager.GetPipeline(observerDetails.Identifier.Value);
        await pipeline.BeginReplay();
    }

    /// <inheritdoc/>
    public async Task EndReplayFor(ObserverDetails observerDetails)
    {
        var @namespace = kernel.GetEventStore((string)observerDetails.Key.MicroserviceId).GetNamespace(observerDetails.Key.TenantId);
        var pipeline = @namespace.ProjectionManager.GetPipeline(observerDetails.Identifier.Value);
        await pipeline.EndReplay();
    }
}
