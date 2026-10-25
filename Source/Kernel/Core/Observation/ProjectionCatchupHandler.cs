// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Projections.Generations;
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
/// <param name="generations">Optional <see cref="IGenerationProjectionResolver"/> that routes observers of a later generation to their immutable execution; absent when the storage provider has no generation catalog.</param>
public class ProjectionCatchupHandler(
    IProjectionsManager projections,
    IProjectionPipelineManager projectionPipelineManager,
    IGrainFactory grainFactory,
    ILogger<ProjectionCatchupHandler> logger,
    IGenerationProjectionResolver? generations = null) : ICanHandleCatchupForObserver
{
    readonly GenerationBulkWindows? _windows = generations is null ? null : new(generations, projectionPipelineManager);

    /// <inheritdoc/>
    public Task<Result<ICanHandleCatchupForObserver.Error>> BeginCatchupFor(ObserverDetails observerDetails) =>
        DoWork(observerDetails, true, async pipeline =>
        {
            await pipeline.BeginBulk();
            return Result<ICanHandleCatchupForObserver.Error>.Success();
        });

    /// <inheritdoc/>
    /// <remarks>
    /// A running catch-up is resumed whenever its observer subscribes again, while its steps keep writing. Beginning a new
    /// window here discarded the writes they had queued (#5340), so resuming joins the open window instead.
    /// </remarks>
    public Task<Result<ICanHandleCatchupForObserver.Error>> ResumeCatchupFor(ObserverDetails observerDetails) =>
        DoWork(observerDetails, true, async pipeline =>
        {
            await pipeline.ResumeBulk();
            return Result<ICanHandleCatchupForObserver.Error>.Success();
        });

    /// <inheritdoc/>
    public Task<Result<ICanHandleCatchupForObserver.Error>> EndCatchupFor(ObserverDetails observerDetails) =>
        DoWork(observerDetails, false, async pipeline =>
        {
            var failedPartitions = (await pipeline.EndBulk()).ToArray();
            _windows?.End(observerDetails.Key);
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
        var generation = await GetGenerationPipeline(observerDetails);
        if (generation is not null)
        {
            var generationResult = await generation.FlushBulk();
            await ProjectionBulkFailures.Record(grainFactory, observerDetails, generationResult.FailedPartitions);
            return generationResult;
        }

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
        var generation = await GetGenerationPipeline(observerDetails);
        if (generation is not null)
        {
            try
            {
                return await generation.EndBulk(cancellationToken);
            }
            finally
            {
                _windows?.End(observerDetails.Key);
            }
        }

        if (!CanHandle(observerDetails) || !projections.TryGet(observerDetails.Key.EventStore, observerDetails.Key.Namespace, observerDetails.Key.ObserverId, out var projection))
        {
            return [];
        }

        var pipeline = await projectionPipelineManager.GetFor(observerDetails.Key.EventStore, observerDetails.Key.Namespace, projection);
        return await pipeline.EndBulk(cancellationToken);
    }

    static bool CanHandle(ObserverDetails observerDetails) => observerDetails.Type == ObserverType.Projection;

    async Task<IProjectionPipeline?> GetGenerationPipeline(ObserverDetails observerDetails, bool beginsWindow = false)
    {
        // A bound generation builds into its own destination from its own immutable execution. Failing to
        // resolve or admit it must not fall back to the current projection, so it throws to the caller.
        if (_windows is null || !CanHandle(observerDetails))
        {
            return null;
        }

        return await _windows.GetPipeline(observerDetails.Key, beginsWindow);
    }

    async Task<Result<ICanHandleCatchupForObserver.Error>> DoWork(
        ObserverDetails observerDetails,
        bool beginsWindow,
        Func<IProjectionPipeline, Task<Result<ICanHandleCatchupForObserver.Error>>> doWork)
    {
        try
        {
            if (!CanHandle(observerDetails))
            {
                return ICanHandleCatchupForObserver.Error.CannotHandle;
            }

            var generation = await GetGenerationPipeline(observerDetails, beginsWindow);
            if (generation is not null)
            {
                return await doWork(generation);
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
