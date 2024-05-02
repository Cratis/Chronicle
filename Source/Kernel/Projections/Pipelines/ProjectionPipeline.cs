// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Changes;
using Cratis.Events;
using Cratis.Kernel.Keys;
using Cratis.Kernel.Storage.Changes;
using Cratis.Kernel.Storage.EventSequences;
using Cratis.Kernel.Storage.Sinks;
using Cratis.Properties;
using Cratis.Schemas;
using Cratis.Types;
using Microsoft.Extensions.Logging;
using EngineProjection = Cratis.Kernel.Projections.IProjection;

namespace Cratis.Kernel.Projections.Pipelines;

/// <summary>
/// Represents an implementation of <see cref="IProjectionPipeline"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="IProjectionPipeline"/>.
/// </remarks>
/// <param name="projection">The <see cref="EngineProjection"/> the pipeline is for.</param>
/// <param name="eventSequenceStorage"><see cref="IEventSequenceStorage"/> to use.</param>
/// <param name="sink"><see cref="ISink"/> to use.</param>
/// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
/// <param name="changesetStorage"><see cref="IChangesetStorage"/> for storing changesets as they occur.</param>
/// <param name="typeFormats"><see cref="ITypeFormats"/> for resolving actual CLR types for schemas.</param>
/// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
public class ProjectionPipeline(
    EngineProjection projection,
    IEventSequenceStorage eventSequenceStorage,
    ISink sink,
    IObjectComparer objectComparer,
    IChangesetStorage changesetStorage,
    ITypeFormats typeFormats,
    ILogger<ProjectionPipeline> logger) : IProjectionPipeline
{
    /// <inheritdoc/>
    public EngineProjection Projection { get; } = projection;

    /// <inheritdoc/>
    public ISink Sink { get; } = sink;

    /// <inheritdoc/>
    public Task BeginReplay() => Sink.BeginReplay();

    /// <inheritdoc/>
    public Task EndReplay() => Sink.EndReplay();

    /// <inheritdoc/>
    public async Task Handle(AppendedEvent @event)
    {
        logger.HandlingEvent(@event.Metadata.SequenceNumber);
        var correlationId = CorrelationId.New(); // TODO: Fix this when we have a proper correlation id
        var keyResolver = Projection.GetKeyResolverFor(@event.Metadata.Type);
        var key = await keyResolver(eventSequenceStorage, @event);
        key = EnsureCorrectTypeForArrayIndexersOnKey(key);
        logger.GettingInitialValues(@event.Metadata.SequenceNumber);
        var initialState = await Sink.FindOrDefault(key);
        initialState ??= Projection.InitialModelState;
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(objectComparer, @event, initialState);
        var context = new ProjectionEventContext(key, @event, changeset);
        await HandleEventFor(Projection, context);
        if (changeset.HasChanges)
        {
            await Sink.ApplyChanges(key, changeset);
            await changesetStorage.Save(correlationId, changeset);
            logger.SavingResult(@event.Metadata.SequenceNumber);
        }
    }

    Key EnsureCorrectTypeForArrayIndexersOnKey(Key key)
    {
        return key with
        {
            ArrayIndexers = new ArrayIndexers(
                key.ArrayIndexers.All.Select(_ =>
                {
                    var originalType = _.Identifier.GetType();
                    var targetType = Projection.Model.Schema.GetTargetTypeForPropertyPath(_.ArrayProperty + _.IdentifierProperty, typeFormats);
                    if (targetType is null)
                    {
                        return _;
                    }

                    return _ with
                    {
                        Identifier = TypeConversion.Convert(targetType, _.Identifier)
                    };
                }))
        };
    }

    async Task HandleEventFor(EngineProjection projection, ProjectionEventContext context)
    {
        if (projection.Accepts(context.Event.Metadata.Type))
        {
            logger.Projecting(context.Event.Metadata.SequenceNumber);
            projection.OnNext(context);
        }
        else
        {
            logger.EventNotAccepted(context.Event.Metadata.SequenceNumber, projection.Name, projection.Path, context.Event.Metadata.Type);
        }
        foreach (var child in projection.ChildProjections)
        {
            if (child.HasKeyResolverFor(context.Event.Metadata.Type))
            {
                var keyResolver = child.GetKeyResolverFor(context.Event.Metadata.Type);
                var key = await keyResolver(eventSequenceStorage, context.Event);
                await HandleEventFor(child, context with { Key = key });
            }
            else
            {
                await HandleEventFor(child, context);
            }
        }
    }
}
