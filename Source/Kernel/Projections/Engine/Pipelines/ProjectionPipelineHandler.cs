// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Reactive.Subjects;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Events.Projections.Changes;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.Projections.Pipelines
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionPipelineHandler"/>.
    /// </summary>
    public class ProjectionPipelineHandler : IProjectionPipelineHandler
    {
        readonly ILogger<ProjectionPipelineHandler> _logger;
        readonly IProjectionPositions _projectionPositions;
        readonly IObjectsComparer _objectsComparer;
        readonly IChangesetStorage _changesetStorage;
        readonly ConcurrentDictionary<ProjectionSinkConfigurationId, EventLogSequenceNumber> _positions = new();
        readonly ReplaySubject<IReadOnlyDictionary<ProjectionSinkConfigurationId, EventLogSequenceNumber>> _observablePositions = new(1);

        /// <inheritdoc/>
        public IObservable<IReadOnlyDictionary<ProjectionSinkConfigurationId, EventLogSequenceNumber>> Positions => _observablePositions;

        /// <inheritdoc/>
        public IReadOnlyDictionary<ProjectionSinkConfigurationId, EventLogSequenceNumber> CurrentPositions => new ReadOnlyDictionary<ProjectionSinkConfigurationId, EventLogSequenceNumber>(_positions);

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectionPipelineHandler"/> class.
        /// </summary>
        /// <param name="objectsComparer"><see cref="IObjectsComparer"/> for comparing objects.</param>
        /// <param name="changesetStorage"><see cref="IChangesetStorage"/> for tracking changesets.</param>
        /// <param name="projectionPositions"><see cref="IProjectionPositions"/> for managing positions for pipelines.</param>
        /// <param name="logger">Logger for logging.</param>
        public ProjectionPipelineHandler(
            IObjectsComparer objectsComparer,
            IChangesetStorage changesetStorage,
            IProjectionPositions projectionPositions,
            ILogger<ProjectionPipelineHandler> logger)
        {
            _objectsComparer = objectsComparer;
            _changesetStorage = changesetStorage;
            _projectionPositions = projectionPositions;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task InitializeFor(IProjectionPipeline pipeline, ProjectionSinkConfigurationId configurationId)
        {
            var offset = await _projectionPositions.GetFor(pipeline.Projection, configurationId);
            UpdatePositionFor(configurationId, offset);
        }

        /// <inheritdoc/>
        public async Task<EventLogSequenceNumber> Handle(AppendedEvent @event, IProjectionPipeline pipeline, IProjectionSink sink, ProjectionSinkConfigurationId configurationId)
        {
            _logger.HandlingEvent(@event.Metadata.SequenceNumber);
            try
            {
                var correlationId = CorrelationId.New();

                var keyResolver = pipeline.Projection.GetKeyResolverFor(@event.Metadata.Type);
                var key = await keyResolver(pipeline.EventProvider, @event);

                _logger.GettingInitialValues(@event.Metadata.SequenceNumber);
                var initialState = await sink.FindOrDefault(key);
                var changeset = new Changeset<AppendedEvent, ExpandoObject>(_objectsComparer, @event, initialState);

                var context = new ProjectionEventContext(key, @event, changeset);
                await HandleEventFor(pipeline.Projection, context);

                var nextSequenceNumber = @event.Metadata.SequenceNumber + 1;
                if (changeset.HasChanges)
                {
                    await sink.ApplyChanges(key, changeset);
                    await _changesetStorage.Save(correlationId, changeset);
                    _logger.SavingResult(@event.Metadata.SequenceNumber);
                }
                await _projectionPositions.Save(pipeline.Projection, configurationId, nextSequenceNumber);
                UpdatePositionFor(configurationId, nextSequenceNumber);
                return nextSequenceNumber;
            }
            catch (Exception ex)
            {
                await pipeline.Suspend($"Exception: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return @event.Metadata.SequenceNumber;
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

        void UpdatePositionFor(ProjectionSinkConfigurationId configurationId, EventLogSequenceNumber offset)
        {
            _positions[configurationId] = offset;
            _observablePositions.OnNext(new ReadOnlyDictionary<ProjectionSinkConfigurationId, EventLogSequenceNumber>(_positions));
        }
    }
}
