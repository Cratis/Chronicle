// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;
using EngineProjection = Cratis.Chronicle.Projections.IProjection;

namespace Cratis.Chronicle.Projections.Pipelines.Steps;

/// <summary>
/// Represents an implementation of <see cref="ICanPerformProjectionPipelineStep" /> that gets the initial state.
/// </summary>
/// <param name="sink"><see cref="ISink" /> used for getting the projected state.</param>
/// <param name="logger"><see cref="ILogger" />.</param>
public class GetInitialState(ISink sink, ILogger<GetInitialState> logger) : ICanPerformProjectionPipelineStep
{
    /// <inheritdoc/>
    public async ValueTask<ProjectionEventContext> Perform(EngineProjection projection, ProjectionEventContext context)
    {
        if (projection.OperationTypescontext.IsJoin || context.IsRemove)
        {
            return context;
        }
        logger.GettingInitialValues(context.Event.Metadata.SequenceNumber);
        var initialState = await sink.FindOrDefault(context.Key);

        var needsInitialState = false;
        if (initialState is null)
        {
            needsInitialState = true;
            initialState = projection.InitialModelState;
        }
        context.Changeset.InitialState = initialState;

        return context with { NeedsInitialState = needsInitialState };
    }
}
