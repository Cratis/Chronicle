// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;
using EngineProjection = Cratis.Chronicle.Projections.IProjection;

namespace Cratis.Chronicle.Projections.Pipelines.Steps;

/// <summary>
/// Represents an implementation of <see cref="ICanPerformProjectionPipelineStep"/> that saves changes.
/// </summary>
/// <param name="sink"><see cref="ISink"/> to use.</param>
/// <param name="changesetStorage"><see cref="IChangesetStorage"/> for storing changesets as they occur.</param>
/// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
public class SaveChanges(ISink sink, IChangesetStorage changesetStorage, ILogger<SaveChanges> logger) : ICanPerformProjectionPipelineStep
{
    /// <inheritdoc/>
    public async ValueTask<ProjectionEventContext> Perform(EngineProjection projection, ProjectionEventContext context)
    {
        // Don't save if the event was deferred (waiting for parent data)
        if (context.IsDeferred)
        {
            logger.NotSavingDueToDeferred(context.Event.Context.SequenceNumber);
            return context;
        }

        if (!context.Changeset.HasChanges)
        {
            logger.NotSaving(context.Event.Context.SequenceNumber);
            return context;
        }
        logger.SavingResult(context.Event.Context.SequenceNumber);

        try
        {
            // TODO: Return the number of affected records and pass this along to the changeset storage
            await sink.ApplyChanges(context.Key, context.Changeset, context.Event.Context.SequenceNumber);
            await changesetStorage.Save(
                projection.ReadModel.Name,
                context.Key,
                context.Event.Context.EventType,
                context.Event.Context.SequenceNumber,
                context.Event.Context.CorrelationId,
                context.Changeset);
        }
        catch (Exception ex)
        {
            var failedPartition = new FailedPartition
            {
                Partition = context.Key,
                ObserverId = ObserverId.Unspecified
            };
            failedPartition.AddAttempt(new FailedPartitionAttempt
            {
                SequenceNumber = context.Event.Context.SequenceNumber,
                Messages = ex.GetAllMessages(),
                StackTrace = ex.StackTrace ?? string.Empty,
                Occurred = DateTimeOffset.UtcNow
            });
            context.AddFailedPartition(failedPartition);
        }

        return context;
    }
}
