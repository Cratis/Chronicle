// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Reactive.Subjects;
using Cratis.Changes;
using Cratis.Events.Projections.Changes;
using Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Cratis.Events.Projections.Pipelines
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionPipelineHandler"/>.
    /// </summary>
    public class ProjectionPipelineHandler : IProjectionPipelineHandler
    {
        readonly ILogger<ProjectionPipelineHandler> _logger;
        readonly IProjectionPositions _projectionPositions;
        readonly IChangesetStorage _changesetStorage;
        readonly ConcurrentDictionary<ProjectionResultStoreConfigurationId, EventLogSequenceNumber> _positions = new();
        readonly ReplaySubject<IReadOnlyDictionary<ProjectionResultStoreConfigurationId, EventLogSequenceNumber>> _observablePositions = new(1);

        public ProjectionPipelineHandler(
            IProjectionPositions projectionPositions,
            IChangesetStorage changesetStorage,
            ILogger<ProjectionPipelineHandler> logger)
        {
            _projectionPositions = projectionPositions;
            _changesetStorage = changesetStorage;
            _logger = logger;
        }

        /// <inheritdoc/>
        public IObservable<IReadOnlyDictionary<ProjectionResultStoreConfigurationId, EventLogSequenceNumber>> Positions => _observablePositions;

        /// <inheritdoc/>
        public IReadOnlyDictionary<ProjectionResultStoreConfigurationId, EventLogSequenceNumber> CurrentPositions => new ReadOnlyDictionary<ProjectionResultStoreConfigurationId, EventLogSequenceNumber>(_positions);

        /// <inheritdoc/>
        public async Task<EventLogSequenceNumber> Handle(Event @event, IProjectionPipeline pipeline, IProjectionResultStore resultStore, ProjectionResultStoreConfigurationId configurationId)
        {
            _logger.HandlingEvent(@event.SequenceNumber);
            try
            {
                var correlationId = CorrelationId.New();
                var changesets = new List<Changeset<Event, ExpandoObject>>();
                await HandleEventFor(pipeline.Projection, resultStore, @event, changesets);
                await _changesetStorage.Save(correlationId, changesets);
                var nextSequenceNumber = @event.SequenceNumber + 1;
                await _projectionPositions.Save(pipeline.Projection, configurationId, nextSequenceNumber);
                UpdatePositionFor(configurationId, nextSequenceNumber);
                return nextSequenceNumber;
            }
            catch (Exception ex)
            {
                await pipeline.Suspend($"Exception: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return @event.SequenceNumber;
            }
        }

        async Task HandleEventFor(IProjection projection, IProjectionResultStore resultStore, Event @event, List<Changeset<Event, ExpandoObject>> changesets)
        {
            var keyResolver = projection.GetKeyResolverFor(@event.Type);
            var key = keyResolver(@event);
            _logger.GettingInitialValues(@event.SequenceNumber);
            var initialState = await resultStore.FindOrDefault(projection.Model, key);
            var changeset = new Changeset<Event, ExpandoObject>(@event, initialState);
            changesets.Add(changeset);
            _logger.Projecting(@event.SequenceNumber);
            projection.OnNext(@event, changeset);
            _logger.SavingResult(@event.SequenceNumber);
            await resultStore.ApplyChanges(projection.Model, key, changeset);

            foreach (var child in projection.ChildProjections)
            {
                await HandleEventFor(child, resultStore, @event, changesets);
            }
        }

        void UpdatePositionFor(ProjectionResultStoreConfigurationId configurationId, EventLogSequenceNumber offset)
        {
            _positions[configurationId] = offset;
            _observablePositions.OnNext(new ReadOnlyDictionary<ProjectionResultStoreConfigurationId, EventLogSequenceNumber>(_positions));
        }
    }
}
