// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
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
/// <param name="projectionFutures"><see cref="IProjectionFutures"/> for managing futures.</param>
/// <param name="typeFormats"><see cref="ITypeFormats"/> for resolving actual CLR types for schemas.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class ResolveFutures(
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    IProjectionFutures projectionFutures,
    ITypeFormats typeFormats,
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
                    var parentIdentifierPath = future.ParentPath + future.ParentIdentifiedByProperty;
                    var type = projection.TargetReadModelSchema.GetTargetTypeForPropertyPath(parentIdentifierPath, typeFormats);
                    var parentKey = future.ParentKey.Value;
                    if (type is not null)
                    {
                        if (type == typeof(Guid))
                        {
                            parentKey = Guid.Parse(parentKey.ToString()!);
                        }
                        else
                        {
                            parentKey = Convert.ChangeType(parentKey.ToString(), type) ?? parentKey;
                        }
                    }

                    // Find the child projection that this future belongs to by navigating the property path
                    var childProjection = FindChildProjectionByPath(projection, future.ChildPath);
                    if (childProjection is null)
                    {
                        logger.FailedToResolveFuture(new InvalidOperationException($"Could not find child projection for path '{future.ChildPath}'"), future.Id, future.ProjectionId);
                        continue;
                    }

                    logger.FoundChildProjection(childProjection.Path, future.Id);
                    logger.CheckingParentInCurrentState(future.Id);

                    if (!ParentExistsInCurrentState(context.Changeset.CurrentState, future.ParentPath, future.ParentIdentifiedByProperty, parentKey))
                    {
                        // Parent still doesn't exist, skip this future
                        logger.ParentNotInCurrentState(future.Id);
                        continue;
                    }

                    var keyResolver = projection.GetKeyResolverFor(future.Event.Context.EventType);

                    logger.ParentExistsInCurrentState(future.Id);
                    logger.ResolvedKeyCallingOnNext(future.ParentKey, future.Id);

                    var childIdentifierPath = future.ChildPath + future.IdentifiedByProperty;
                    var childType = projection.TargetReadModelSchema.GetTargetTypeForPropertyPath(childIdentifierPath, typeFormats);

                    object childKey = future.Event.Context.EventSourceId;

                    if (childType == typeof(Guid))
                    {
                        childKey = Guid.Parse(future.Event.Context.EventSourceId);
                    }

                    var key = new Key(parentKey, new ArrayIndexers(
                    [
                        new ArrayIndexer(future.ParentPath, future.ParentIdentifiedByProperty, parentKey),
                        new ArrayIndexer(future.ChildPath, future.IdentifiedByProperty, childKey)
                    ]));

                    var futureContext = context with
                    {
                        Event = future.Event,
                        Key = key
                    };

                    var contextEvent = futureContext.Changeset.Incoming;
                    futureContext.Changeset.Incoming = future.Event;
                    childProjection.OnNext(futureContext);
                    futureContext.Changeset.Incoming = contextEvent;

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
        // Check if this projection's ChildrenPropertyPath matches the target path
        if (projection.ChildrenPropertyPath.Path == childPath.Path)
        {
            return projection;
        }

        // Recursively search through child projections
        foreach (var child in projection.ChildProjections)
        {
            var found = FindChildProjectionByPath(child, childPath);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    static bool ParentExistsInCurrentState(ExpandoObject? currentState, PropertyPath parentChildrenPath, PropertyPath parentIdentifiedByProperty, object parentKey)
    {
        if (currentState is null)
        {
            return false;
        }

        var idPath = parentChildrenPath + parentIdentifiedByProperty;
        var arrayIndex = new ArrayIndexer(parentChildrenPath, parentIdentifiedByProperty, parentKey);

        return idPath.HasValue(currentState, new([arrayIndex]));
    }
}
