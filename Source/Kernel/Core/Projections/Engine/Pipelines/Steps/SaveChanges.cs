// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
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
            // overwrite earlier pendingSaves' mutations. Fold same-key pendingSaves whose
            // changes are *purely* PropertiesChanged into the main changeset so the sink sees
            // one composite write per key.
            //
            // PendingSaves containing ChildAdded / ChildRemoved are left for their own
            // ApplyChanges call: MongoDB cannot push a child into an array element that is
            // being pushed in the same update (see the comment in ResolveFutures), and the
            // per-call write also keeps SQL correct because the separate call reads the just-
            // written document state via the same key.
            var pendingSavesToFold = context.PendingFutureSaves
                .Where(p => Equals(p.Key.Value, context.Key.Value)
                    && p.Changeset.Changes.All(c => c is PropertiesChanged<ExpandoObject>))
                .ToArray();
            foreach (var pendingSave in pendingSavesToFold)
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

        var foldedSet = context.Changeset.HasChanges
            ? new HashSet<PendingFutureSave>(context.PendingFutureSaves
                .Where(p => Equals(p.Key.Value, context.Key.Value)
                    && p.Changeset.Changes.All(c => c is PropertiesChanged<ExpandoObject>)))
            : [];
        foreach (var pendingSave in context.PendingFutureSaves.Where(p => !foldedSet.Contains(p)))
        {
            await sink.ApplyChanges(pendingSave.Key, pendingSave.Changeset, context.Event.Context.SequenceNumber);
        }

        return context;
    }
}
