// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.Logging;
using EngineProjection = Cratis.Chronicle.Projections.IProjection;

namespace Cratis.Chronicle.Projections.Pipelines.Steps;

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

        if (projection.Accepts(context.Event.Metadata.Type))
        {
            logger.Projecting(context.Event.Metadata.SequenceNumber);
            projection.OnNext(context);
        }
        else
        {
            logger.EventNotAccepted(context.Event.Metadata.SequenceNumber, projection.Identifier, projection.Path, context.Event.Metadata.Type);
        }
        foreach (var child in projection.ChildProjections)
        {
            if (child.HasKeyResolverFor(context.Event.Metadata.Type))
            {
                var keyResolver = child.GetKeyResolverFor(context.Event.Metadata.Type);
                var key = await keyResolver(eventSequenceStorage, context.Event);
                await Perform(child, context with { Key = key });
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
