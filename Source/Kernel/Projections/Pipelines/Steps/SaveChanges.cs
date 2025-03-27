// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        if (!context.Changeset.HasChanges)
        {
            logger.NotSaving(context.Event.Metadata.SequenceNumber);
            return context;
        }
        logger.SavingResult(context.Event.Metadata.SequenceNumber);

        // TODO: Return the number of affected records and pass this along to the changeset storage
        await sink.ApplyChanges(context.Key, context.Changeset, context.Event.Metadata.SequenceNumber);
        await changesetStorage.Save(
            projection.Model.Name,
            context.Key,
            context.Event.Metadata.Type,
            context.Event.Context.SequenceNumber,
            context.Event.Context.CorrelationId,
            context.Changeset);

        return context;
    }
}
