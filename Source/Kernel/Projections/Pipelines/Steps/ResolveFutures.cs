// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Types;
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
                        parentKey = TypeConversion.Convert(type, parentKey);
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

                    // Check if parent exists in the current changeset's state
                    // For direct children (parentPath is root), we need to look in the child collection
                    // For nested children, we look in the parent's collection
                    if (!ParentExistsInCurrentState(context.Changeset.CurrentState, future.ParentPath, future.ChildPath, future.ParentIdentifiedByProperty, parentKey))
                    {
                        // Parent still doesn't exist, skip this future
                        logger.ParentNotInCurrentState(future.Id);
                        continue;
                    }

                    logger.ParentExistsInCurrentState(future.Id);
                    logger.ResolvedKeyCallingOnNext(future.ParentKey, future.Id);

                    var childIdentifierPath = future.ChildPath + future.IdentifiedByProperty;
                    var childType = projection.TargetReadModelSchema.GetTargetTypeForPropertyPath(childIdentifierPath, typeFormats);

                    // Extract the child key from the event content using the IdentifiedByProperty path
                    // This ensures we get the correct identifier (e.g., HubId) rather than using EventSourceId
                    // which might be the parent's identifier when UsingParentKeyFromContext is used.
                    // If the property doesn't exist in the event content (returns null), and the IdentifiedByProperty
                    // is a simple property name (not a nested path), fall back to using EventSourceId.
                    // This handles cases like WeightsSetForSimulationConfiguration where the ConfigurationId
                    // is the EventSourceId and not in the event content.
                    var childKey = future.IdentifiedByProperty.GetValue(future.Event.Content, ArrayIndexers.NoIndexers);
                    if (childKey is null && !future.IdentifiedByProperty.Path.Contains('.') && !future.IdentifiedByProperty.Path.Contains('['))
                    {
                        // Property doesn't exist in event content and it's a simple property name,
                        // so use EventSourceId (which is how the event was originally sourced)
                        childKey = future.Event.Context.EventSourceId;
                    }

                    if (childType is not null && childKey is not null)
                    {
                        childKey = TypeConversion.Convert(childType, childKey);
                    }

                    // Build the key for the future event
                    // Use the current context's root key and add array indexers for navigating to the child
                    var key = new Key(context.Key.Value, new ArrayIndexers(
                    [
                        new ArrayIndexer(future.ParentPath, future.ParentIdentifiedByProperty, parentKey),
                        new ArrayIndexer(future.ChildPath, future.IdentifiedByProperty, childKey!)
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

    static bool ParentExistsInCurrentState(ExpandoObject? currentState, PropertyPath parentChildrenPath, PropertyPath childPath, PropertyPath parentIdentifiedByProperty, object parentKey)
    {
        if (currentState is null)
        {
            return false;
        }

        // For direct children (parentChildrenPath is root/not set), we need to use childPath to locate the parent in the array
        // For nested children, parentChildrenPath already points to the correct location
        var arrayPath = (parentChildrenPath.IsSet && !parentChildrenPath.IsRoot) ? parentChildrenPath : childPath;

        // Use PropertyPath.GetValue to navigate to the collection - it handles ExpandoObject navigation properly
        var collectionValue = arrayPath.GetValue(currentState, ArrayIndexers.NoIndexers);

        if (collectionValue is null)
        {
            return false;
        }

        // Try to cast to IEnumerable<ExpandoObject>, handling both List<object> and IEnumerable<ExpandoObject>
        IEnumerable<ExpandoObject>? collection;
        if (collectionValue is IEnumerable<ExpandoObject> expandoCollection)
        {
            collection = expandoCollection;
        }
        else if (collectionValue is IEnumerable enumerable)
        {
            collection = enumerable.OfType<ExpandoObject>();
        }
        else
        {
            return false;
        }

        // Check if the collection contains an item with the matching identifier
        return collection.ToList().Contains(parentIdentifiedByProperty, parentKey);
    }
}
