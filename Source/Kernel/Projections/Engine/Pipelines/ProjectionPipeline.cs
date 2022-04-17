// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Events.Projections.Changes;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.Projections.Pipelines;

/// <summary>
/// Represents an implementation of <see cref="IProjectionPipeline"/>.
/// </summary>
public class ProjectionPipeline : IProjectionPipeline
{
    readonly IObjectsComparer _objectsComparer;
    readonly IChangesetStorage _changesetStorage;
    readonly ILogger<ProjectionPipeline> _logger;

    /// <inheritdoc/>
    public IProjection Projection { get; }

    /// <inheritdoc/>
    public IProjectionEventProvider EventProvider { get; }

    /// <inheritdoc/>
    public IProjectionSink Sink { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="IProjectionPipeline"/>.
    /// </summary>
    /// <param name="projection">The <see cref="IProjection"/> the pipeline is for.</param>
    /// <param name="eventProvider"><see cref="IProjectionEventProvider"/> to use.</param>
    /// <param name="sink"><see cref="IProjectionSink"/> to use.</param>
    /// <param name="objectsComparer"><see cref="IObjectsComparer"/> for comparing objects.</param>
    /// <param name="changesetStorage"><see cref="IChangesetStorage"/> for storing changesets as they occur.</param>
    /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
    public ProjectionPipeline(
        IProjection projection,
        IProjectionEventProvider eventProvider,
        IProjectionSink sink,
        IObjectsComparer objectsComparer,
        IChangesetStorage changesetStorage,
        ILogger<ProjectionPipeline> logger)
    {
        EventProvider = eventProvider;
        Sink = sink;
        _objectsComparer = objectsComparer;
        _changesetStorage = changesetStorage;
        Projection = projection;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Handle(AppendedEvent @event)
    {
        _logger.HandlingEvent(@event.Metadata.SequenceNumber);
        var correlationId = CorrelationId.New();
        var keyResolver = Projection.GetKeyResolverFor(@event.Metadata.Type);
        var key = await keyResolver(EventProvider, @event);
        _logger.GettingInitialValues(@event.Metadata.SequenceNumber);
        var initialState = await Sink.FindOrDefault(key);
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(_objectsComparer, @event, initialState);
        var context = new ProjectionEventContext(key, @event, changeset);
        await HandleEventFor(Projection, context);
        if (changeset.HasChanges)
        {
            await Sink.ApplyChanges(key, changeset);
            await _changesetStorage.Save(correlationId, changeset);
            _logger.SavingResult(@event.Metadata.SequenceNumber);
        }
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
