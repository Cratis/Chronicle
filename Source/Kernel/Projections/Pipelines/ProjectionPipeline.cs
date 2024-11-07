// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Projections.Pipelines.Steps;
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
/// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
/// <param name="steps">Collection of <see cref="ICanPerformProjectionPipelineStep"/> to perform.</param>
/// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
public class ProjectionPipeline(
    EngineProjection projection,
    ISink sink,
    IObjectComparer objectComparer,
    IEnumerable<ICanPerformProjectionPipelineStep> steps,
    ILogger<ProjectionPipeline> logger) : IProjectionPipeline
{
    /// <inheritdoc/>
    public EngineProjection Projection { get; } = projection;

    /// <inheritdoc/>
    public ISink Sink { get; } = sink;

    /// <inheritdoc/>
    public Task BeginReplay() => Sink.BeginReplay();

    /// <inheritdoc/>
    public Task EndReplay() => Sink.EndReplay();

    /// <inheritdoc/>
    public async Task Handle(AppendedEvent @event)
    {
        logger.StartingPipeline(@event.Metadata.SequenceNumber);

        var changeset = new Changeset<AppendedEvent, ExpandoObject>(objectComparer, @event, new ExpandoObject());
        var context = new ProjectionEventContext(Key.Undefined, @event, changeset);
        foreach (var step in steps)
        {
            context = await step.Perform(Projection, context);
        }
    }
}
