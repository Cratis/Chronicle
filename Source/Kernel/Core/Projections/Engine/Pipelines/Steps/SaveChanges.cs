// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;
using EngineProjection = Cratis.Chronicle.Projections.IProjection;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps;

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

        // TODO: Return the number of affected records and pass this along to the changeset storage
        var failedPartitions = await sink.ApplyChanges(context.Key, context.Changeset, context.Event.Context.SequenceNumber);

        if (failedPartitions.Any())
        {
            foreach (var failedPartition in failedPartitions)
            {
                context.AddFailedPartition(failedPartition);
            }

            return context;
        }

        await changesetStorage.Save(
            projection.ReadModel.ContainerName,
            context.Key,
            context.Event.Context.EventType,
            context.Event.Context.SequenceNumber,
            context.Event.Context.CorrelationId,
            context.Changeset);

        return context;
    }
}
