// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Pipelines;
using Cratis.Monads;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="ICanHandleCatchupForObserver"/> for projections.
/// </summary>
/// <param name="projections"><see cref="IProjectionsManager"/> for managing projections.</param>
/// <param name="projectionPipelineManager"><see cref="IProjectionPipelineManager"/> for managing projection pipelines.</param>
/// <param name="logger">The logger.</param>
public class ProjectionCatchupHandler(
    IProjectionsManager projections,
    IProjectionPipelineManager projectionPipelineManager,
    ILogger<ProjectionCatchupHandler> logger) : ICanHandleCatchupForObserver
{
    /// <inheritdoc/>
    public Task<Result<ICanHandleCatchupForObserver.Error>> BeginCatchupFor(ObserverDetails observerDetails) =>
        DoWork(observerDetails, pipeline => pipeline.BeginBulk());

    /// <inheritdoc/>
    public Task<Result<ICanHandleCatchupForObserver.Error>> ResumeCatchupFor(ObserverDetails observerDetails) =>
        DoWork(observerDetails, pipeline => pipeline.BeginBulk());

    /// <inheritdoc/>
    public Task<Result<ICanHandleCatchupForObserver.Error>> EndCatchupFor(ObserverDetails observerDetails) =>
        DoWork(observerDetails, pipeline => pipeline.EndBulk());

    static bool CanHandle(ObserverDetails observerDetails) => observerDetails.Type == ObserverType.Projection;

    async Task<Result<ICanHandleCatchupForObserver.Error>> DoWork(
        ObserverDetails observerDetails,
        Func<IProjectionPipeline, Task> doWork)
    {
        try
        {
            if (!CanHandle(observerDetails))
            {
                return ICanHandleCatchupForObserver.Error.CannotHandle;
            }

            if (!projections.TryGet(observerDetails.Key.EventStore, observerDetails.Key.Namespace, observerDetails.Key.ObserverId, out var projection))
            {
                return Result<ICanHandleCatchupForObserver.Error>.Success();
            }

            var pipeline = projectionPipelineManager.GetFor(observerDetails.Key.EventStore, observerDetails.Key.Namespace, projection);
            await doWork(pipeline);
            return Result<ICanHandleCatchupForObserver.Error>.Success();
        }
        catch (Exception ex)
        {
            logger.FailedToHandleCatchup(ex, observerDetails.Key.ObserverId, observerDetails.Type);
            return ICanHandleCatchupForObserver.Error.Unknown;
        }
    }
}
