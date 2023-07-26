// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Engines.Changes;
using Aksio.Cratis.Kernel.Engines.Keys;
using Aksio.Cratis.Kernel.Engines.Sinks;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Schemas;
using Aksio.Types;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Engines.Projections.Pipelines;

/// <summary>
/// Represents an implementation of <see cref="IProjectionPipeline"/>.
/// </summary>
public class ProjectionPipeline : IProjectionPipeline
{
    readonly IObjectComparer _objectComparer;
    readonly IChangesetStorage _changesetStorage;
    readonly ITypeFormats _typeFormats;
    readonly ILogger<ProjectionPipeline> _logger;
    readonly IEventSequenceStorage _eventProvider;

    /// <inheritdoc/>
    public IProjection Projection { get; }

    /// <inheritdoc/>
    public ISink Sink { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="IProjectionPipeline"/>.
    /// </summary>
    /// <param name="projection">The <see cref="IProjection"/> the pipeline is for.</param>
    /// <param name="eventProvider"><see cref="IEventSequenceStorage"/> to use.</param>
    /// <param name="sink"><see cref="ISink"/> to use.</param>
    /// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
    /// <param name="changesetStorage"><see cref="IChangesetStorage"/> for storing changesets as they occur.</param>
    /// <param name="typeFormats"><see cref="ITypeFormats"/> for resolving actual CLR types for schemas.</param>
    /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
    public ProjectionPipeline(
        IProjection projection,
        IEventSequenceStorage eventProvider,
        ISink sink,
        IObjectComparer objectComparer,
        IChangesetStorage changesetStorage,
        ITypeFormats typeFormats,
        ILogger<ProjectionPipeline> logger)
    {
        _eventProvider = eventProvider;
        Sink = sink;
        _objectComparer = objectComparer;
        _changesetStorage = changesetStorage;
        _typeFormats = typeFormats;
        Projection = projection;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Handle(AppendedEvent @event)
    {
        if (@event.Context.ObservationState.HasFlag(EventObservationState.HeadOfReplay))
        {
            await Sink.BeginReplay();
        }
        var isReplaying = @event.Context.ObservationState.HasFlag(EventObservationState.Replay);

        _logger.HandlingEvent(@event.Metadata.SequenceNumber);
        var correlationId = CorrelationId.New();
        var keyResolver = Projection.GetKeyResolverFor(@event.Metadata.Type);
        var key = await keyResolver(_eventProvider, @event);
        key = EnsureCorrectTypeForArrayIndexersOnKey(key);
        _logger.GettingInitialValues(@event.Metadata.SequenceNumber);
        var initialState = await Sink.FindOrDefault(key, isReplaying);
        initialState ??= Projection.InitialModelState;
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(_objectComparer, @event, initialState);
        var context = new ProjectionEventContext(key, @event, changeset);
        await HandleEventFor(Projection, context);
        if (changeset.HasChanges)
        {
            await Sink.ApplyChanges(key, changeset, isReplaying);
            await _changesetStorage.Save(correlationId, changeset);
            _logger.SavingResult(@event.Metadata.SequenceNumber);
        }

        if (@event.Context.ObservationState.HasFlag(EventObservationState.TailOfReplay))
        {
            await Sink.EndReplay();
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
                    var targetType = Projection.Model.Schema.GetTargetTypeForPropertyPath(_.ArrayProperty + _.IdentifierProperty, _typeFormats);
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

    async Task HandleEventFor(IProjection projection, ProjectionEventContext context)
    {
        if (projection.Accepts(context.Event.Metadata.Type))
        {
            _logger.Projecting(context.Event.Metadata.SequenceNumber);
            projection.OnNext(context);
        }
        else
        {
            _logger.EventNotAccepted(context.Event.Metadata.SequenceNumber, projection.Name, projection.Path, context.Event.Metadata.Type);
        }
        foreach (var child in projection.ChildProjections)
        {
            if (child.HasKeyResolverFor(context.Event.Metadata.Type))
            {
                var keyResolver = child.GetKeyResolverFor(context.Event.Metadata.Type);
                var key = await keyResolver(_eventProvider, context.Event);
                await HandleEventFor(child, context with { Key = key });
            }
            else
            {
                await HandleEventFor(child, context);
            }
        }
    }
}
