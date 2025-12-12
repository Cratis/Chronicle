// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;
using EngineProjection = Cratis.Chronicle.Projections.IProjection;

namespace Cratis.Chronicle.Projections.Pipelines.Steps;

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
        // Don't set initial state if the event was deferred (key is undefined)
        if (context.IsDeferred)
        {
            return context;
        }

        if (context.IsJoin)
        {
            return context;
        }

        logger.GettingInitialValues(context.Event.Context.SequenceNumber);

        // If we are joining, or adding a child - we do want to set initial state
        // We can then set a property __initialized to false if its not already
        // For other operations, when we get the object from the sink, if the object exists and __initialized is false, we can set the initial state
        // for those properties that does not have a value. We then set the __initialized to true.
        var initialState = await sink.FindOrDefault(context.Key);

        var needsInitialState = false;
        if (initialState is null)
        {
            if (context.ChildrenAffected || context.IsJoin)
            {
                initialState = new ExpandoObject();
                ((IDictionary<string, object?>)initialState)[WellKnownProperties.ReadModelInstanceInitialized] = false;
                context.Changeset.SetInitialized(false);
            }
            else
            {
                needsInitialState = true;
                initialState = projection.InitialModelState.Clone();
                context.Changeset.SetInitialized(true);
            }

            SetKeyForInitialState(projection, initialState, context.Key);
        }
        else if (!HasBeenInitialized(initialState))
        {
            var initialStateAsDictionary = (IDictionary<string, object?>)initialState;
            var initialModelStateAsDictionary = (IDictionary<string, object?>)projection.InitialModelState;

            // TODO: Ideally we should do this recursively, as properties can be joined deep in the hierarchy and we want to
            // initialize all properties that are not set. This is a simple implementation that only works for the first level.
            foreach (var property in initialModelStateAsDictionary.Where((kvp) => !initialStateAsDictionary.ContainsKey(kvp.Key)))
            {
                initialStateAsDictionary[property.Key] = property.Value;
            }

            context.Changeset.SetInitialized(true);
        }

        context.Changeset.InitialState = initialState;

        return context with { NeedsInitialState = needsInitialState };
    }

    bool HasBeenInitialized(ExpandoObject initialState) =>
        ((IDictionary<string, object?>)initialState).TryGetValue(WellKnownProperties.ReadModelInstanceInitialized, out var initialized) && initialized is bool initializedBool && initializedBool;

    void SetKeyForInitialState(EngineProjection projection, ExpandoObject initialState, Key key)
    {
        // TODO: We should improve how we work with Keys and not just "magic strings" like id or _id (MongoDB):
        // https://github.com/Cratis/Chronicle/issues/1387
        // https://github.com/Cratis/Chronicle/issues/1630
        var keyPropertyName = projection.TargetReadModelSchema.HasKeyProperty() ? projection.TargetReadModelSchema.GetKeyProperty().Name : projection.TargetReadModelSchema.GetLikelyKeyPropertyName();
        ((IDictionary<string, object?>)initialState)[keyPropertyName] = key.Value;
    }
}
