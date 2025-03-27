// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections.Pipelines.Steps;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;
using EngineProjection = Cratis.Chronicle.Projections.IProjection;

namespace Cratis.Chronicle.Projections.Pipelines;

/// <summary>
/// Represents an implementation of <see cref="IProjectionPipeline"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="IProjectionPipeline"/>.
/// </remarks>
/// <param name="projection">The <see cref="EngineProjection"/> the pipeline is for.</param>
/// <param name="sink"><see cref="ISink"/> to use.</param>
/// <param name="changesetStorage"><see cref="IChangesetStorage"/> for storing changesets as they occur.</param>
/// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
/// <param name="steps">Collection of <see cref="ICanPerformProjectionPipelineStep"/> to perform.</param>
/// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
public class ProjectionPipeline(
    EngineProjection projection,
    ISink sink,
    IChangesetStorage changesetStorage,
    IObjectComparer objectComparer,
    IEnumerable<ICanPerformProjectionPipelineStep> steps,
    ILogger<ProjectionPipeline> logger) : IProjectionPipeline
{
    /// <inheritdoc/>
    public async Task BeginReplay(ReplayContext context)
    {
        await changesetStorage.BeginReplay(projection.Model.Name);
        await sink.BeginReplay(context);
    }

    /// <inheritdoc/>
    public Task ResumeReplay(ReplayContext context) => sink.ResumeReplay(context);

    /// <inheritdoc/>
    public async Task EndReplay(ReplayContext context)
    {
        await sink.EndReplay(context);
        await changesetStorage.EndReplay(projection.Model.Name);
    }

    /// <inheritdoc/>
    public async Task<IChangeset<AppendedEvent, ExpandoObject>> Handle(AppendedEvent @event)
    {
        logger.StartingPipeline(@event.Metadata.SequenceNumber);
        var context = ProjectionEventContext.Empty(objectComparer, @event) with
        {
            OperationType = projection.GetOperationTypeFor(@event.Metadata.Type),

        };

        foreach (var step in steps)
        {
            try
            {
                context = await step.Perform(projection, context);
            }
            catch (Exception ex)
            {
                logger.ErrorPerformingStep(ex, step.GetType(), @event.Metadata.SequenceNumber);
                throw;
            }
        }
        logger.CompletedAllSteps(@event.Metadata.SequenceNumber);

        return context.Changeset;
    }
}
