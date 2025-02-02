// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.Logging;
using EngineProjection = Cratis.Chronicle.ProjectionEngine.IProjection;

namespace Cratis.Chronicle.ProjectionEngine.Pipelines.Steps;

/// <summary>
/// Represents an implementation of <see cref="ICanPerformProjectionPipelineStep"/> that handles an event.
/// </summary>
/// <param name="eventSequenceStorage"><see cref="IEventSequenceStorage" /> storage for event sequences.</param>
/// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
public class HandleEvent(IEventSequenceStorage eventSequenceStorage, ILogger<HandleEvent> logger) : ICanPerformProjectionPipelineStep
{
    /// <inheritdoc/>
    public async ValueTask<ProjectionEventContext> Perform(EngineProjection projection, ProjectionEventContext context)
    {
        logger.HandlingEvent(context.Event.Metadata.SequenceNumber);
        var eventType = context.Event.Metadata.Type;
        if (projection.Accepts(eventType))
        {
            logger.Projecting(context.Event.Metadata.SequenceNumber);
            projection.OnNext(context);
        }
        else
        {
            logger.EventNotAccepted(context.Event.Metadata.SequenceNumber, projection.Identifier, projection.Path, eventType);
        }
        foreach (var child in projection.ChildProjections)
        {
            if (child.HasKeyResolverFor(eventType))
            {
                var keyResolver = child.GetKeyResolverFor(eventType);
                var key = await keyResolver(eventSequenceStorage, context.Event);
                await Perform(child, context with { Key = key, OperationType = child.OperationTypes[eventType] });
            }
            else
            {
                await Perform(child, context);
            }
        }

        if (context.NeedsInitialState && !context.Changeset.HasJoined())
        {
            context.Changeset.AddPropertiesFrom(projection.InitialModelState);
        }

        return context;
    }
}
