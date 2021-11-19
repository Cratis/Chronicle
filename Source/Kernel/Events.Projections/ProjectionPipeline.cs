// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
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
        readonly ConcurrentDictionary<ProjectionResultStoreConfigurationId, IProjectionResultStore> _resultStores = new();
        readonly ConcurrentDictionary<ProjectionResultStoreConfigurationId, ISubject<Event>> _subjectsPerConfiguration = new();
        readonly ConcurrentDictionary<ProjectionResultStoreConfigurationId, IDisposable> _subscriptionsPerConfiguration = new();
        readonly ConcurrentDictionary<ProjectionResultStoreConfigurationId, CancellationTokenSource> _cancellationTokenSourcePerConfiguration = new();
        readonly ConcurrentDictionary<ProjectionResultStoreConfigurationId, bool> _rewindsPerConfiguration = new();
        readonly ConcurrentDictionary<ProjectionResultStoreConfigurationId, EventLogSequenceNumber> _positions = new();
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
            State = ProjectionState.Registering;
        }

        /// <inheritdoc/>
        public IProjection Projection { get; }

        /// <inheritdoc/>
        public IProjectionEventProvider EventProvider { get; }

        /// <inheritdoc/>
        public IEnumerable<IProjectionResultStore> ResultStores => _resultStores.Values;

        /// <inheritdoc/>
        public ProjectionState State { get; private set; }

        /// <inheritdoc/>
        public IReadOnlyDictionary<ProjectionResultStoreConfigurationId, EventLogSequenceNumber> Positions => _positions;

        /// <inheritdoc/>
        public void Start()
        {
            foreach (var (configurationId, resultStore) in _resultStores)
            {
                StartForConfigurationAndResultStore(configurationId, resultStore, _rewindsPerConfiguration.ContainsKey(configurationId));
            }
        }

        /// <inheritdoc/>
        public void Pause()
        {
            _logger.Pausing(Projection.Identifier);
            State = ProjectionState.Paused;
            foreach (var (configurationId, _) in _resultStores)
            {
                StopForConfiguration(configurationId);
            }
        }

        /// <inheritdoc/>
        public void Resume()
        {
            _logger.Resuming(Projection.Identifier);
            State = ProjectionState.Active;
            foreach (var (configurationId, resultStore) in _resultStores)
            {
                StartForConfigurationAndResultStore(configurationId, resultStore, true);
            }
        }

        /// <inheritdoc/>
        public void Rewind()
        {
            _logger.Rewinding(Projection.Identifier);
            State = ProjectionState.Rewinding;
            foreach (var (configurationId, _) in _resultStores)
            {
                Rewind(configurationId);
            }
        }

        /// <inheritdoc/>
        public void Rewind(ProjectionResultStoreConfigurationId configurationId)
        {
            _logger.RewindingForConfiguration(Projection.Identifier, configurationId);
            _rewindsPerConfiguration[configurationId] = true;

            if (State != ProjectionState.Rewinding)
            {
                State = ProjectionState.PartialRewinding;
            }

            if (State != ProjectionState.Registering)
            {
                var resultStore = _resultStores[configurationId];
                StartForConfigurationAndResultStore(configurationId, resultStore, true);
            }
        }

        /// <inheritdoc/>
        public void StoreIn(ProjectionResultStoreConfigurationId configurationId, IProjectionResultStore resultStore)
        {
            _resultStores[configurationId] = resultStore;
            _subjectsPerConfiguration[configurationId] = new ReplaySubject<Event>();
        }

        void StopForConfiguration(ProjectionResultStoreConfigurationId configurationId)
        {
            if (_subscriptionsPerConfiguration.ContainsKey(configurationId)) _subscriptionsPerConfiguration[configurationId].Dispose();
            if (_cancellationTokenSourcePerConfiguration.ContainsKey(configurationId)) _cancellationTokenSourcePerConfiguration[configurationId].Cancel();
            _subscriptionsPerConfiguration.Remove(configurationId, out _);
            _cancellationTokenSourcePerConfiguration.Remove(configurationId, out _);
        }

        void StartForConfigurationAndResultStore(ProjectionResultStoreConfigurationId configurationId, IProjectionResultStore resultStore, bool rewind)
        {
            StopForConfiguration(configurationId);

            var cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSourcePerConfiguration[configurationId] = cancellationTokenSource;

            Task.Run(async () =>
            {
                try
                {
                    IProjectionResultStoreRewindScope? rewindScope = null;
                    if (rewind)
                    {
                        rewindScope = await PrepareRewind(configurationId);
                    }

                    await CatchUp(configurationId, resultStore, cancellationTokenSource.Token);

                    rewindScope?.Dispose();
                    _rewindsPerConfiguration.Remove(configurationId, out _);

                    var subject = _subjectsPerConfiguration[configurationId];
                    EventProvider.ProvideFor(Projection, subject);
                    _subscriptionsPerConfiguration[configurationId] = subject.Subscribe(@event => OnNext(@event, resultStore, configurationId).Wait());
                    State = ProjectionState.Active;
                }
                catch (Exception ex)
                {
                    _logger.ErrorStartingProviding(Projection.Identifier, ex);
                    State = ProjectionState.Failed;
                }
            }, cancellationTokenSource.Token);
        }

        async Task<IProjectionResultStoreRewindScope> PrepareRewind(ProjectionResultStoreConfigurationId configurationId)
        {
            State = ProjectionState.Rewinding;
            var resultStore = _resultStores[configurationId];
            var scope = resultStore.BeginRewindFor(Projection.Model);
            await _projectionPositions.Reset(Projection, configurationId);
            return scope;
        }

        async Task CatchUp(
            ProjectionResultStoreConfigurationId configurationId,
            IProjectionResultStore resultStore,
            CancellationToken cancellationToken)
        {
            _logger.CatchingUp(Projection.Identifier, configurationId);
            var offset = await _projectionPositions.GetFor(Projection, configurationId);
            if (offset == 0)
            {
                await resultStore.PrepareInitialRun(Projection.Model);
            }
            _positions[configurationId] = offset;

            var exhausted = false;
            State = ProjectionState.CatchingUp;

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
                        if (cancellationToken.IsCancellationRequested) return;
                        offset = await OnNext(@event, resultStore, configurationId);
                        _positions[configurationId] = offset;
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
