// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;
using EngineProjection = Cratis.Chronicle.Projections.IProjection;

namespace Cratis.Chronicle.Projections.Pipelines.Steps;

/// <summary>
/// Represents an implementation of <see cref="ICanPerformProjectionPipelineStep"/> that resolves pending futures.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ResolveFutures"/> class.
/// </remarks>
/// <param name="eventStore">The <see cref="EventStoreName"/> for the event store.</param>
/// <param name="namespace">The <see cref="EventStoreNamespaceName"/> for the namespace.</param>
/// <param name="eventSequenceStorage"><see cref="IEventSequenceStorage"/> for event sequences.</param>
/// <param name="sink"><see cref="ISink"/> for querying the read model.</param>
/// <param name="projectionFutures"><see cref="IProjectionFutures"/> for managing futures.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class ResolveFutures(
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    IEventSequenceStorage eventSequenceStorage,
    ISink sink,
    IProjectionFutures projectionFutures,
    ILogger<ResolveFutures> logger) : ICanPerformProjectionPipelineStep
{
    /// <inheritdoc/>
    public async ValueTask<ProjectionEventContext> Perform(EngineProjection projection, ProjectionEventContext context)
    {
        // If this event was deferred, don't try to resolve futures (we have no valid data yet)
        if (context.IsDeferred)
        {
            return context;
        }

        // Attempt to resolve any pending futures now that we've processed a new event
        var futures = await projectionFutures.GetFutures(eventStore, @namespace, projection.Identifier);
        if (!futures.Any())
        {
            return context;
        }

        logger.FoundPendingFutures(futures.Count(), projection.Identifier);

        // Keep trying to resolve futures until we can't resolve any more
        // This handles the case where resolving one future creates the parent data needed by another future
        var resolvedAny = true;
        while (resolvedAny)
        {
            resolvedAny = false;
            futures = await projectionFutures.GetFutures(eventStore, @namespace, projection.Identifier);

            foreach (var future in futures)
            {
                try
                {
                    // Find the child projection that this future belongs to by navigating the property path
                    var childProjection = FindChildProjectionByPath(projection, future.ChildPath);
                    if (childProjection is null)
                    {
                        logger.FailedToResolveFuture(new InvalidOperationException($"Could not find child projection for path '{future.ChildPath}'"), future.Id, future.ProjectionId);
                        continue;
                    }

                    logger.FoundChildProjection(childProjection.Path, future.Id);

                    // Check if the parent exists in the current state
                    logger.CheckingParentInCurrentState(future.Id);
                    if (!ParentExistsInCurrentState(context.Changeset.CurrentState, future.ParentPath, future.ParentIdentifiedByProperty, future.ParentKeyValue))
                    {
                        // Parent still doesn't exist, skip this future
                        logger.ParentNotInCurrentState(future.Id);
                        continue;
                    }

                    logger.ParentExistsInCurrentState(future.Id);

                    // Resolve the key for the child using the child projection's key resolver
                    var keyResolver = childProjection.GetKeyResolverFor(future.Event.Context.EventType);
                    var keyResult = await keyResolver(eventSequenceStorage, sink, future.Event);

                    // If key resolution is still deferred, skip this future
                    if (keyResult is DeferredKey)
                    {
                        logger.KeyResolutionDeferred(future.Id);
                        continue;
                    }

                    var key = (keyResult as ResolvedKey)!.Key;
                    logger.ResolvedKeyCallingOnNext(key.Value.ToString() ?? "null", future.Id);

                    // Call OnNext with the resolved key - this adds the child to the parent in the changeset
                    var futureContext = context with { Event = future.Event, Key = key };
                    childProjection.OnNext(futureContext);

                    // Successfully resolved the future
                    await projectionFutures.ResolveFuture(eventStore, @namespace, projection.Identifier, future.Id);
                    logger.ResolvedFuture(future.Id, future.ProjectionId);
                    resolvedAny = true;
                }
                catch (Exception ex)
                {
                    logger.FailedToResolveFuture(ex, future.Id, future.ProjectionId);
                }
            }
        }

        return context;
    }

    static EngineProjection? FindChildProjectionByPath(EngineProjection projection, PropertyPath childPath)
    {
        // Navigate through the projection hierarchy to find the child projection
        // ChildPath is like "Configurations.Hubs" - navigate from root down to the leaf
        var current = projection;

        foreach (var segment in childPath.Segments)
        {
            // Find the child projection whose ChildrenPropertyPath matches this segment
            var nextChild = current.ChildProjections.FirstOrDefault(c => c.ChildrenPropertyPath.Path == segment.ToString());
            if (nextChild is null)
            {
                return null;
            }
            current = nextChild;
        }

        return current;
    }

    static bool ParentExistsInCurrentState(ExpandoObject? currentState, string parentChildrenPath, string parentIdentifiedByProperty, object parentKey)
    {
        if (currentState is null)
        {
            return false;
        }

        // Navigate through the current state to find the parent collection
        var pathSegments = parentChildrenPath.Split('.');

        object? current = currentState;
        foreach (var segment in pathSegments)
        {
            if (current is not IDictionary<string, object?> dict || !dict.TryGetValue(segment, out var value))
            {
                return false;
            }
            current = value;
        }

        // current should now be a collection of parent items
        if (current is not IEnumerable<object> collection)
        {
            return false;
        }

        // Check if any item in the collection has the matching parent key in the specified property
        foreach (var item in collection)
        {
            if (item is IDictionary<string, object?> itemDict &&
                itemDict.TryGetValue(parentIdentifiedByProperty, out var value) &&
                Equals(value, parentKey))
            {
                return true;
            }
        }

        return false;
    }
}
