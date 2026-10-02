// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps;

/// <summary>
/// Represents an implementation of <see cref="ICanPerformProjectionPipelineStep"/> that decrypts
/// compliance (<c language="csharp">[PII]</c>) and security (<c language="csharp">[Encrypted]</c>) fields in the initial state using
/// the stored <see cref="WellKnownProperties.Subject"/> field.
/// </summary>
/// <param name="readModelsCompliance">The <see cref="IReadModelsCompliance"/> for decrypting the fields.</param>
/// <param name="eventStore">The <see cref="EventStoreName"/> this step belongs to.</param>
/// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/> this step belongs to.</param>
public class DecryptInitialState(
    IReadModelsCompliance readModelsCompliance,
    EventStoreName eventStore,
    EventStoreNamespaceName eventStoreNamespace) : ICanPerformProjectionPipelineStep
{
    /// <inheritdoc/>
    public async ValueTask<ProjectionEventContext> Perform(IProjection projection, ProjectionEventContext context)
    {
        if (context.Changeset.InitialState is null)
        {
            return context;
        }

        var schema = projection.TargetReadModelSchema;
        if (!schema.HasSchemaMetadata())
        {
            return context;
        }

        if (!((IDictionary<string, object?>)context.Changeset.InitialState).ContainsKey(WellKnownProperties.Subject))
        {
            return context;
        }

        var initialState = (IDictionary<string, object?>)context.Changeset.InitialState;
        initialState.TryGetValue(WellKnownProperties.Subject, out var storedSubject);
        initialState.TryGetValue(WellKnownProperties.Subjects, out var storedSubjects);
        initialState.TryGetValue(WellKnownProperties.ReadModelInstanceInitialized, out var initialized);

        var released = await readModelsCompliance.Release(
            eventStore,
            eventStoreNamespace,
            schema,
            context.Changeset.InitialState);
        var releasedState = (IDictionary<string, object?>)released;
        releasedState[WellKnownProperties.Subject] = storedSubject;
        if (storedSubjects is not null)
        {
            releasedState[WellKnownProperties.Subjects] = storedSubjects;
        }

        // Release's schema conversion must not turn an absent placeholder property into a default,
        // or lose the flag that keeps subsequent events in a bulk window from initializing the root.
        if (initialized is false)
        {
            var present = initialState.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var property in releasedState.Keys.Where(property => !present.Contains(property) && !WellKnownProperties.All.Contains(property)).ToArray())
            {
                releasedState.Remove(property);
            }
        }
        if (initialized is not null)
        {
            releasedState[WellKnownProperties.ReadModelInstanceInitialized] = initialized;
        }

        context.Changeset.InitialState = released;
        return context;
    }
}
