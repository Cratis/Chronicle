// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps;

/// <summary>
/// Represents an implementation of <see cref="ICanPerformProjectionPipelineStep"/> that handles an event.
/// </summary>
/// <param name="eventSequenceStorage"><see cref="IEventSequenceStorage" /> storage for event sequences.</param>
/// <param name="sink"><see cref="ISink"/> for querying the read model.</param>
/// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
public class HandleEvent(IEventSequenceStorage eventSequenceStorage, ISink sink, ILogger<HandleEvent> logger) : ICanPerformProjectionPipelineStep
{
    /// <inheritdoc/>
    public async ValueTask<ProjectionEventContext> Perform(IProjection projection, ProjectionEventContext context)
    {
        logger.HandlingEvent(context.Event.Context.SequenceNumber);

        if (context.IsUnresolvable)
        {
            logger.SkippingDueToUnresolvableKey(context.Event.Context.SequenceNumber, projection.Identifier, projection.Path);
            return context;
        }

        var eventType = context.Event.Context.EventType;
        if (projection.Accepts(eventType))
        {
            logger.Projecting(context.Event.Context.SequenceNumber);
            projection.OnNext(context);
        }
        else
        {
            logger.EventNotAccepted(context.Event.Context.SequenceNumber, projection.Identifier, projection.Path, eventType);
        }
        foreach (var child in projection.ChildProjections)
        {
            logger.ProcessingChildProjection(child.Path, eventType.Id, context.Event.Context.SequenceNumber);
            if (child.HasKeyResolverFor(eventType))
            {
                var keyResolver = child.GetKeyResolverFor(eventType);
                var keyResult = await keyResolver(eventSequenceStorage, sink, context.Event);

                // Handle deferred key resolution for child projections
                if (keyResult is DeferredKey deferredChild)
                {
                    logger.ChildKeyResolutionDeferred(child.Path, eventType.Id, context.Event.Context.SequenceNumber);
                    context.AddDeferredFuture(deferredChild.Future);
                    continue;
                }

                // Handle permanently unresolvable key — skip this child with no future created
                if (keyResult is UnresolvableKey)
                {
                    logger.ChildKeyUnresolvable(child.Path, eventType.Id, context.Event.Context.SequenceNumber);
                    continue;
                }

                var key = (keyResult as ResolvedKey)!.Key;
                logger.ChildHasKeyResolver(child.Path, eventType.Id, key.Value);
                var operationType = child.GetOperationTypeFor(eventType);
                await Perform(child, context with { Key = key, OperationType = operationType });
            }
            else
            {
                logger.ChildNoKeyResolver(child.Path, eventType.Id);
                await Perform(child, context);
            }
        }

        if (context.NeedsInitialState && !context.Changeset.HasJoined())
        {
            // Only add properties from InitialModelState for root projections
            // Child projections should not add their InitialModelState to the shared changeset
            // because those properties belong to the child level, not the root level
            if (projection.ChildrenPropertyPath.IsRoot)
            {
                // Collect every children collection path the projection owns so the initial-state
                // diff doesn't $set them to empty. Replay races sibling partitions through their own
                // events independently: an unconstrained Features=[] from the root's ModuleCreated
                // would otherwise erase whatever a FeatureCreated event already pushed onto Features
                // from its own partition.
                var childrenPaths = CollectChildrenPropertyPaths(projection).ToArray();
                context.Changeset.AddPropertiesFrom(projection.InitialModelState, childrenPaths);
            }
        }

        return context;
    }

    /// <summary>
    /// Walks the projection tree and returns every children-collection path the root projection owns.
    /// </summary>
    /// <param name="projection">Root <see cref="IProjection"/> to walk.</param>
    /// <returns>The set of children property paths.</returns>
    static IEnumerable<PropertyPath> CollectChildrenPropertyPaths(IProjection projection)
    {
        foreach (var child in projection.ChildProjections)
        {
            if (!child.ChildrenPropertyPath.IsRoot)
            {
                yield return child.ChildrenPropertyPath;
            }

            foreach (var deeper in CollectChildrenPropertyPaths(child))
            {
                yield return deeper;
            }
        }
    }
}
