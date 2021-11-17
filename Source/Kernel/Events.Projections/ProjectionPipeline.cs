// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Subjects;
using Cratis.Changes;
using Cratis.Events.Projections.Changes;
using Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionPipeline"/>.
    /// </summary>
    public class ProjectionPipeline : IProjectionPipeline
    {
        readonly Dictionary<ProjectionResultStoreConfigurationId, IProjectionResultStore> _resultStores = new();
        readonly Dictionary<ProjectionResultStoreConfigurationId, ISubject<Event>> _subjectsPerConfiguration = new();
        readonly IProjectionPositions _projectionPositions;
        readonly IChangesetStorage _changesetStorage;
        readonly ILogger<ProjectionPipeline> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="IProjectionPipeline"/>.
        /// </summary>
        /// <param name="projection">The <see cref="IProjection"/> the pipeline is for.</param>
        /// <param name="eventProvider"><see cref="IProjectionEventProvider"/> to use.</param>
        /// <param name="projectionPositions"><see cref="IProjectionPositions"/> to use.</param>
        /// <param name="changesetStorage"><see cref="IChangesetStorage"/> for storing changesets as they occur.</param>
        /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
        public ProjectionPipeline(
            IProjection projection,
            IProjectionEventProvider eventProvider,
            IProjectionPositions projectionPositions,
            IChangesetStorage changesetStorage,
            ILogger<ProjectionPipeline> logger)
        {
            EventProvider = eventProvider;
            _projectionPositions = projectionPositions;
            Projection = projection;
            _changesetStorage = changesetStorage;
            _logger = logger;
        }

        /// <inheritdoc/>
        public IProjectionEventProvider EventProvider { get; }

        /// <inheritdoc/>
        public IProjection Projection { get; }

        /// <inheritdoc/>
        public IEnumerable<IProjectionResultStore> ResultStores => _resultStores.Values;

        /// <inheritdoc/>
        public void Start()
        {
            foreach (var (configurationId, resultStore) in _resultStores)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await CatchUp(configurationId, resultStore);

                        var subject = _subjectsPerConfiguration[configurationId];
                        EventProvider.ProvideFor(Projection, subject);
                        subject.Subscribe(@event => OnNext(@event, resultStore, configurationId).Wait());
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorStartingProviding(Projection.Identifier, ex);
                    }
                });
            }
        }

        /// <inheritdoc/>
        public void Pause()
        {
            _logger.Pausing(Projection.Identifier);
        }

        /// <inheritdoc/>
        public void Resume()
        {
            _logger.Resuming(Projection.Identifier);
        }

        /// <inheritdoc/>
        public Task Rewind()
        {
            _logger.Rewinding(Projection.Identifier);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task Rewind(ProjectionResultStoreConfigurationId configurationId) => throw new NotImplementedException();

        /// <inheritdoc/>
        public void StoreIn(ProjectionResultStoreConfigurationId configurationId, IProjectionResultStore resultStore)
        {
            _resultStores[configurationId] = resultStore;
            _subjectsPerConfiguration[configurationId] = new ReplaySubject<Event>();
        }

        async Task CatchUp(ProjectionResultStoreConfigurationId configurationId, IProjectionResultStore resultStore)
        {
            _logger.CatchingUp(Projection.Identifier, configurationId);
            var offset = await _projectionPositions.GetFor(Projection, configurationId);

            var exhausted = false;

            while (!exhausted)
            {
                var cursor = await EventProvider.GetFromPosition(Projection, offset);
                while (await cursor.MoveNext())
                {
                    if (!cursor.Current.Any())
                    {
                        exhausted = true;
                        break;
                    }

                    foreach (var @event in cursor.Current)
                    {
                        offset = await OnNext(@event, resultStore, configurationId);
                    }
                }
                if (!cursor.Current.Any()) exhausted = true;
            }
        }

        async Task<EventLogSequenceNumber> OnNext(Event @event, IProjectionResultStore resultStore, ProjectionResultStoreConfigurationId configurationId)
        {
            _logger.HandlingEvent(@event.SequenceNumber);
            var correlationId = CorrelationId.New();
            var changesets = new List<Changeset<Event, ExpandoObject>>();
            await HandleEventFor(Projection, resultStore, @event, changesets);
            await _changesetStorage.Save(correlationId, changesets);
            var nextSequenceNumber = @event.SequenceNumber + 1;
            await _projectionPositions.Save(Projection, configurationId, nextSequenceNumber);
            return nextSequenceNumber;
        }

        async Task HandleEventFor(IProjection projection, IProjectionResultStore resultStore, Event @event, List<Changeset<Event, ExpandoObject>> changesets)
        {
            var keyResolver = projection.GetKeyResolverFor(@event.Type);
            var key = keyResolver(@event);
            _logger.GettingInitialValues(@event.SequenceNumber);
            var initialState = await resultStore.FindOrDefault(Projection.Model, key);
            var changeset = new Changeset<Event, ExpandoObject>(@event, initialState);
            changesets.Add(changeset);
            _logger.Projecting(@event.SequenceNumber);
            projection.OnNext(@event, changeset);
            _logger.SavingResult(@event.SequenceNumber);
            await resultStore.ApplyChanges(Projection.Model, key, changeset);

            foreach (var child in projection.ChildProjections)
            {
                await HandleEventFor(child, resultStore, @event, changesets);
            }
        }
    }
}
