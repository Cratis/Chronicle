// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Events.Projections.Changes;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Json;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Types;
using Microsoft.Extensions.Logging;
using NJsonSchema;

namespace Aksio.Cratis.Events.Projections.Pipelines;

/// <summary>
/// Represents an implementation of <see cref="IProjectionPipeline"/>.
/// </summary>
public class ProjectionPipeline : IProjectionPipeline
{
    readonly IObjectsComparer _objectsComparer;
    readonly IChangesetStorage _changesetStorage;
    readonly ITypeFormats _typeFormats;
    readonly ILogger<ProjectionPipeline> _logger;
    readonly IEventSequenceStorageProvider _eventProvider;

    /// <inheritdoc/>
    public IProjection Projection { get; }

    /// <inheritdoc/>
    public IProjectionSink Sink { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="IProjectionPipeline"/>.
    /// </summary>
    /// <param name="projection">The <see cref="IProjection"/> the pipeline is for.</param>
    /// <param name="eventProvider"><see cref="IEventSequenceStorageProvider"/> to use.</param>
    /// <param name="sink"><see cref="IProjectionSink"/> to use.</param>
    /// <param name="objectsComparer"><see cref="IObjectsComparer"/> for comparing objects.</param>
    /// <param name="changesetStorage"><see cref="IChangesetStorage"/> for storing changesets as they occur.</param>
    /// <param name="typeFormats"><see cref="ITypeFormats"/> for resolving actual CLR types for schemas.</param>
    /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
    public ProjectionPipeline(
        IProjection projection,
        IEventSequenceStorageProvider eventProvider,
        IProjectionSink sink,
        IObjectsComparer objectsComparer,
        IChangesetStorage changesetStorage,
        ITypeFormats typeFormats,
        ILogger<ProjectionPipeline> logger)
    {
        _eventProvider = eventProvider;
        Sink = sink;
        _objectsComparer = objectsComparer;
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

        _logger.HandlingEvent(@event.Metadata.SequenceNumber);
        var correlationId = CorrelationId.New();
        var keyResolver = Projection.GetKeyResolverFor(@event.Metadata.Type);
        var key = await keyResolver(_eventProvider, @event);
        key = EnsureCorrectTypeForArrayIndexersOnKey(key);
        _logger.GettingInitialValues(@event.Metadata.SequenceNumber);
        var initialState = await Sink.FindOrDefault(key);
        initialState ??= Projection.InitialModelState;
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(_objectsComparer, @event, initialState);
        var context = new ProjectionEventContext(key, @event, changeset);
        await HandleEventFor(Projection, context);
        if (changeset.HasChanges)
        {
            await Sink.ApplyChanges(key, changeset);
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
                    var targetType = _.Identifier.GetType();
                    var originalType = targetType;
                    var schemaProperty = Projection.Model.Schema.GetSchemaPropertyForPropertyPath(_.ArrayProperty + _.IdentifierProperty);
                    if (schemaProperty is not null)
                    {
                        if (_typeFormats.IsKnown(schemaProperty.Format))
                        {
                            targetType = _typeFormats.GetTypeForFormat(schemaProperty.Format);
                        }
                        else
                        {
                            targetType = schemaProperty.Type switch
                            {
                                JsonObjectType.String => typeof(string),
                                JsonObjectType.Boolean => typeof(bool),
                                JsonObjectType.Integer => typeof(int),
                                JsonObjectType.Null => typeof(double),
                                _ => targetType
                            };
                        }
                    }

                    return targetType == originalType ? _ : _ with
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
            await HandleEventFor(child, context);
        }
    }
}
