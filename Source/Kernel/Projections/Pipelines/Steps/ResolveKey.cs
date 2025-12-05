// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Types;
using Microsoft.Extensions.Logging;
using EngineProjection = Cratis.Chronicle.Projections.IProjection;

namespace Cratis.Chronicle.Projections.Pipelines.Steps;

/// <summary>
/// Represents an implementation of <see cref="ICanPerformProjectionPipelineStep"/> that resolves the key for an event.
/// </summary>
/// <param name="eventSequenceStorage"><see cref="IEventSequenceStorage"/> to potentially resolve keys from.</param>
/// <param name="sink"><see cref="ISink"/> for querying the read model.</param>
/// <param name="typeFormats"><see cref="ITypeFormats"/>.</param>
/// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
public class ResolveKey(IEventSequenceStorage eventSequenceStorage, ISink sink, ITypeFormats typeFormats, ILogger<ResolveKey> logger) : ICanPerformProjectionPipelineStep
{
    /// <inheritdoc/>
    public async ValueTask<ProjectionEventContext> Perform(EngineProjection projection, ProjectionEventContext context)
    {
        logger.ResolvingKey(context.Event.Context.SequenceNumber);
        var keyResolver = projection.GetKeyResolverFor(context.Event.Context.EventType);
        var keyResult = await keyResolver(eventSequenceStorage, sink, context.Event);

        // Handle deferred key resolution - this means the parent wasn't found in Sink yet
        if (keyResult is DeferredKey deferredKey)
        {
            logger.KeyResolutionDeferred(context.Event.Context.SequenceNumber, projection.Identifier, projection.Path);
            context.AddDeferredFuture(deferredKey.Future);
            return context;
        }

        var key = (keyResult as ResolvedKey)!.Key;
        key = EnsureCorrectTypeForArrayIndexersOnKey(projection, key);
        return context with { Key = key };
    }

    Key EnsureCorrectTypeForArrayIndexersOnKey(EngineProjection projection, Key key) =>
        key with
        {
            ArrayIndexers = new ArrayIndexers(
                key.ArrayIndexers.All.Select(arrayIndexer =>
                {
                    var targetType = projection.TargetReadModelSchema.GetTargetTypeForPropertyPath(arrayIndexer.ArrayProperty + arrayIndexer.IdentifierProperty, typeFormats);
                    if (targetType is null)
                    {
                        return arrayIndexer;
                    }

                    return arrayIndexer with
                    {
                        Identifier = TypeConversion.Convert(targetType, arrayIndexer.Identifier)
                    };
                }))
        };
}
