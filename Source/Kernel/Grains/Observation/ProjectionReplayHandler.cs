// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Pipelines;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="ICanHandleReplayForObserver"/> for projections.
/// </summary>
/// <param name="projectionManager"><see cref="IProjectionManager"/> for managing projections.</param>
/// <param name="projectionPipelineManager"><see cref="IProjectionPipelineManager"/> for managing projection pipelines.</param>
/// <param name="logger">The logger.</param>
public class ProjectionReplayHandler(
    IProjectionManager projectionManager,
    IProjectionPipelineManager projectionPipelineManager,
    ILogger<ProjectionReplayHandler> logger) : ICanHandleReplayForObserver
{
    /// <inheritdoc/>
    public async Task<Result<ICanHandleReplayForObserver.Error>> BeginReplayFor(ObserverDetails observerDetails) => await DoWorkOnPipeline(observerDetails, pipeline => pipeline.BeginReplay());

    /// <inheritdoc/>
    public async Task<Result<ICanHandleReplayForObserver.Error>> ResumeReplayFor(ObserverDetails observerDetails) => await DoWorkOnPipeline(observerDetails, pipeline => pipeline.ResumeReplay());

    /// <inheritdoc/>
    public async Task<Result<ICanHandleReplayForObserver.Error>> EndReplayFor(ObserverDetails observerDetails) => await DoWorkOnPipeline(observerDetails, pipeline => pipeline.EndReplay());

    static bool CanHandle(ObserverDetails observerDetails) => observerDetails.Type == ObserverType.Projection;

    async Task<Result<ICanHandleReplayForObserver.Error>> DoWorkOnPipeline(ObserverDetails observerDetails, Func<IProjectionPipeline, Task> doWork)
    {
        try
        {
            if (!CanHandle(observerDetails))
            {
                return ICanHandleReplayForObserver.Error.CannotHandle;
            }

            if (projectionManager.TryGet(observerDetails.Key.EventStore, observerDetails.Key.Namespace, observerDetails.Identifier, out var projection))
            {
                var pipeline = projectionPipelineManager.GetFor(observerDetails.Key.EventStore, observerDetails.Key.Namespace, projection);
                await doWork(pipeline);
            }
            return Result<ICanHandleReplayForObserver.Error>.Success();
        }
        catch (Exception ex)
        {
            logger.Failed(ex, observerDetails.Identifier, observerDetails.Type);
            return ICanHandleReplayForObserver.Error.Unknown;
        }
    }
}