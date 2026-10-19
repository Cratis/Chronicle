// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="ICanHandleCatchupForObserver"/> for projections.
/// </summary>
/// <param name="projections"><see cref="IProjectionsManager"/> for managing projections.</param>
/// <param name="projectionPipelineManager"><see cref="IProjectionPipelineManager"/> for managing projection pipelines.</param>
/// <param name="grainFactory">The grain factory used to record failed partitions.</param>
/// <param name="logger">The logger.</param>
public class ProjectionCatchupHandler(
    IProjectionsManager projections,
    IProjectionPipelineManager projectionPipelineManager,
    IGrainFactory grainFactory,
    ILogger<ProjectionCatchupHandler> logger) : ICanHandleCatchupForObserver
{
    /// <inheritdoc/>
    public Task<Result<ICanHandleCatchupForObserver.Error>> BeginCatchupFor(ObserverDetails observerDetails) =>
        DoWork(observerDetails, async pipeline =>
        {
            await pipeline.BeginBulk();
            return Result<ICanHandleCatchupForObserver.Error>.Success();
        });

    /// <inheritdoc/>
    public Task<Result<ICanHandleCatchupForObserver.Error>> ResumeCatchupFor(ObserverDetails observerDetails) =>
        DoWork(observerDetails, async pipeline =>
        {
            await pipeline.BeginBulk();
            return Result<ICanHandleCatchupForObserver.Error>.Success();
        });

    /// <inheritdoc/>
    public Task<Result<ICanHandleCatchupForObserver.Error>> EndCatchupFor(ObserverDetails observerDetails) =>
        DoWork(observerDetails, async pipeline =>
        {
            var failedPartitions = (await pipeline.EndBulk()).ToArray();
            await ProjectionBulkFailures.Record(grainFactory, observerDetails, failedPartitions);

            // An ordinary write failure is recorded and retried from the event that failed, so catch-up still completes.
            // Only a final flush whose outcome is unknown fails it: writes before the failure may have been lost.
            return failedPartitions.Any(partition => partition.OutcomeUnknown)
                ? ICanHandleCatchupForObserver.Error.Unknown
                : Result<ICanHandleCatchupForObserver.Error>.Success();
        });

    /// <inheritdoc/>
    public async Task<BulkFlushResult> FlushFor(ObserverDetails observerDetails)
    {
        if (!CanHandle(observerDetails) || !projections.TryGet(observerDetails.Key.EventStore, observerDetails.Key.Namespace, observerDetails.Key.ObserverId, out var projection))
        {
            return new(null, []);
        }

        var pipeline = await projectionPipelineManager.GetFor(observerDetails.Key.EventStore, observerDetails.Key.Namespace, projection);
        var result = await pipeline.FlushBulk();
        await ProjectionBulkFailures.Record(grainFactory, observerDetails, result.FailedPartitions);
        return result;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Storage.Sinks.FailedPartition>> EndBulkFor(ObserverDetails observerDetails, CancellationToken cancellationToken)
    {
        if (!CanHandle(observerDetails) || !projections.TryGet(observerDetails.Key.EventStore, observerDetails.Key.Namespace, observerDetails.Key.ObserverId, out var projection))
        {
            return [];
        }

        var pipeline = await projectionPipelineManager.GetFor(observerDetails.Key.EventStore, observerDetails.Key.Namespace, projection);
        return await pipeline.EndBulk(cancellationToken);
    }

    static bool CanHandle(ObserverDetails observerDetails) => observerDetails.Type == ObserverType.Projection;

    async Task<Result<ICanHandleCatchupForObserver.Error>> DoWork(
        ObserverDetails observerDetails,
        Func<IProjectionPipeline, Task<Result<ICanHandleCatchupForObserver.Error>>> doWork)
    {
        try
        {
            if (!CanHandle(observerDetails))
            {
                return ICanHandleCatchupForObserver.Error.CannotHandle;
            }

            var projection = await projections.GetCurrent(observerDetails.Key.EventStore, observerDetails.Key.Namespace, observerDetails.Key.ObserverId);
            if (projection is null)
            {
                logger.ProjectionMissing(observerDetails.Key);
                return ICanHandleCatchupForObserver.Error.Unknown;
            }

            var pipeline = await projectionPipelineManager.GetFor(observerDetails.Key.EventStore, observerDetails.Key.Namespace, projection);
            return await doWork(pipeline);
        }
        catch (Exception ex)
        {
            logger.Failed(ex, observerDetails.Key.ObserverId, observerDetails.Type);
            return ICanHandleCatchupForObserver.Error.Unknown;
        }
    }
}
