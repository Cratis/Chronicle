// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps;

/// <summary>
/// Represents an implementation of <see cref="ICanPerformProjectionPipelineStep" /> that gets the initial state.
/// </summary>
/// <param name="sink"><see cref="ISink" /> used for getting the projected state.</param>
/// <param name="logger"><see cref="ILogger" />.</param>
public class SetInitialState(ISink sink, ILogger<SetInitialState> logger) : ICanPerformProjectionPipelineStep
{
    /// <inheritdoc/>
    public async ValueTask<ProjectionEventContext> Perform(IProjection projection, ProjectionEventContext context)
    {
        // Don't set initial state if the event was deferred or the key is permanently unresolvable
        if (context.IsDeferred || context.IsUnresolvable)
        {
            return context;
        }

        // For join events, initial state is resolved via the join key resolution path — skip here.
        if (context.IsJoin)
        {
            return context;
        }

        // Don't set initial state if the key value could not be resolved.
        if (context.Key.Value is null)
        {
            return context;
        }

        logger.GettingInitialValues(context.Event.Context.SequenceNumber);

        // If we are joining, or adding a child - we do want to set initial state
        // We can then set a property __initialized to false if its not already
        // For other operations, when we get the object from the sink, if the object exists and __initialized is false, we can set the initial state
        // for those properties that does not have a value. We then set the __initialized to true.
        var initialState = await sink.FindOrDefault(context.Key);

        // When the sink has no instance for the key yet, this event creates the read model instance.
        context.IsNewInstance = initialState is null;

        var needsInitialState = false;
        if (initialState is null)
        {
            if (!context.CreatesInstance)
            {
                initialState = new ExpandoObject();
                ((IDictionary<string, object?>)initialState)[WellKnownProperties.ReadModelInstanceInitialized] = false;
                context.Changeset.SetInitialized(false);
            }
            else
            {
                needsInitialState = true;
                initialState = projection.InitialModelState.Clone();
                ((IDictionary<string, object?>)initialState)[WellKnownProperties.ReadModelInstanceInitialized] = true;
                context.Changeset.SetInitialized(true);
            }

            SetKeyForInitialState(projection, initialState, context.Key);
        }
        else if (!HasBeenInitialized(initialState))
        {
            // A sink that stores the flag reports a root only children have touched as explicitly not initialized, and it
            // stays that way until an event the root handles arrives. A sink that does not store the flag reports nothing
            // about it, so every existing instance is initialized by the next event, as it always was.
            var storedAsNotInitialized = IsStoredAsNotInitialized(initialState);
            if (storedAsNotInitialized && !context.CreatesInstance)
            {
                context.Changeset.InitialState = initialState;
                return context with { NeedsInitialState = false };
            }

            var initialStateAsDictionary = (IDictionary<string, object?>)initialState;
            var initialModelStateAsDictionary = (IDictionary<string, object?>)projection.InitialModelState;

            // TODO: Ideally we should do this recursively, as properties can be joined deep in the hierarchy and we want to
            // initialize all properties that are not set. This is a simple implementation that only works for the first level.
            var missing = initialModelStateAsDictionary
                .Where(kvp => !initialStateAsDictionary.ContainsKey(kvp.Key))
                .ToArray();

            foreach (var property in missing)
            {
                initialStateAsDictionary[property.Key] = property.Value;
            }

            // The sink only stores what the changeset records, so the values filled in for a placeholder have to be
            // recorded as changes. Children collections are owned by the events that add and remove their children,
            // and the key is set by the sink, so neither is recorded from the initial state.
            if (storedAsNotInitialized)
            {
                var childrenRoots = projection.GetChildrenPropertyPaths()
                    .Where(_ => !_.IsRoot)
                    .Select(_ => _.Segments.First().Value)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var keyPropertyName = GetKeyPropertyName(projection);
                var recorded = missing
                    .Where(kvp => !childrenRoots.Contains(kvp.Key) && !string.Equals(kvp.Key, keyPropertyName, StringComparison.OrdinalIgnoreCase))
                    .Select(_ => new PropertyDifference(new PropertyPath(_.Key), null, _.Value))
                    .ToArray();
                if (recorded.Length > 0)
                {
                    context.Changeset.Add(new PropertiesChanged<ExpandoObject>(context.Changeset.CurrentState, recorded));
                }
            }

            // Bulk sinks cache CurrentState between events, before the recorded differences reach storage.
            initialStateAsDictionary[WellKnownProperties.ReadModelInstanceInitialized] = true;
            context.Changeset.SetInitialized(true);
        }

        context.Changeset.InitialState = initialState;

        return context with { NeedsInitialState = needsInitialState };
    }

    bool HasBeenInitialized(ExpandoObject initialState) =>
        ((IDictionary<string, object?>)initialState).TryGetValue(WellKnownProperties.ReadModelInstanceInitialized, out var initialized) && initialized is bool initializedBool && initializedBool;

    bool IsStoredAsNotInitialized(ExpandoObject initialState) =>
        ((IDictionary<string, object?>)initialState).TryGetValue(WellKnownProperties.ReadModelInstanceInitialized, out var initialized) && initialized is false;

    void SetKeyForInitialState(IProjection projection, ExpandoObject initialState, Key key)
    {
        // TODO: We should improve how we work with Keys and not just "magic strings" like id or _id (MongoDB):
        // https://github.com/Cratis/Chronicle/issues/1387
        // https://github.com/Cratis/Chronicle/issues/1630
        ((IDictionary<string, object?>)initialState)[GetKeyPropertyName(projection)] = key.Value;
    }

    string GetKeyPropertyName(IProjection projection) =>
        projection.TargetReadModelSchema.HasKeyProperty() ? projection.TargetReadModelSchema.GetKeyProperty().Name : projection.TargetReadModelSchema.GetLikelyKeyPropertyName();
}
