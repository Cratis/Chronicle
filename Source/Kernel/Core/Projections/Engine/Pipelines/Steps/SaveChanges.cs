// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;

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
    public async ValueTask<ProjectionEventContext> Perform(IProjection projection, ProjectionEventContext context)
    {
        // Don't save if the event was deferred or the key is permanently unresolvable
        if (context.IsDeferred || context.IsUnresolvable)
        {
            logger.NotSavingDueToDeferred(context.Event.Context.SequenceNumber);
            return context;
        }

        var hasPendingFutureSaves = context.PendingFutureSaves.Any();

        if (!context.Changeset.HasChanges && !hasPendingFutureSaves)
        {
            logger.NotSaving(context.Event.Context.SequenceNumber);
            return context;
        }

        if (context.Changeset.HasChanges)
        {
            logger.SavingResult(context.Event.Context.SequenceNumber);

            // Sinks that serialize a whole document per ApplyChanges call (the SQL sink does;
            // MongoDB instead uses field-level $set operators that compose naturally) cannot
            // safely process the main changeset plus several pendingSaves for the same key as
            // separate operations — each pendingSave starts from its own InitialState clone,
            // so later pendingSaves serialize on top of the unmutated initial document and
            // overwrite earlier pendingSaves' mutations. When pendingSaves target the same
            // key as the main changeset, fold their Changes into the main changeset so the
            // sink sees one composite write per key.
            var samePendingSaves = context.PendingFutureSaves
                .Where(p => Equals(p.Key.Value, context.Key.Value))
                .ToArray();
            foreach (var pendingSave in samePendingSaves)
            {
                foreach (var change in pendingSave.Changeset.Changes)
                {
                    context.Changeset.Add(change);
                }
            }

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
        }

        var remainingPendingSaves = context.Changeset.HasChanges
            ? context.PendingFutureSaves.Where(p => !Equals(p.Key.Value, context.Key.Value))
            : context.PendingFutureSaves;
        foreach (var pendingSave in remainingPendingSaves)
        {
            await sink.ApplyChanges(pendingSave.Key, pendingSave.Changeset, context.Event.Context.SequenceNumber);
        }

        return context;
    }
}
