// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;
using EngineProjection = Cratis.Chronicle.ProjectionEngine.IProjection;

namespace Cratis.Chronicle.ProjectionEngine.Pipelines.Steps;

/// <summary>
/// Represents an implementation of <see cref="ICanPerformProjectionPipelineStep" /> that gets the initial state.
/// </summary>
/// <param name="sink"><see cref="ISink" /> used for getting the projected state.</param>
/// <param name="logger"><see cref="ILogger" />.</param>
public class SetInitialState(ISink sink, ILogger<SetInitialState> logger) : ICanPerformProjectionPipelineStep
{
    /// <inheritdoc/>
    public async ValueTask<ProjectionEventContext> Perform(EngineProjection projection, ProjectionEventContext context)
    {
        if (context.IsJoin)
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
            SetKeyForInitialState(initialState, context.Key);
        }
        context.Changeset.InitialState = initialState;

        return context with { NeedsInitialState = needsInitialState };
    }

    void SetKeyForInitialState(ExpandoObject initialState, Key key)
    {
        // TODO: We should improve how we work with Keys and not just "magic strings" like id or _id (MongoDB):
        // https://github.com/Cratis/Chronicle/issues/1387
        // https://github.com/Cratis/Chronicle/issues/1630
        ((IDictionary<string, object?>)initialState)["id"] = key.Value;
    }
}
