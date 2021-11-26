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
        readonly ConcurrentDictionary<ProjectionResultStoreConfigurationId, bool> _catchUpPerConfiguration = new();
        readonly IProjectionPositions _projectionPositions;
        readonly IChangesetStorage _changesetStorage;
        readonly ILogger<ProjectionPipeline> _logger;
        readonly BehaviorSubject<ProjectionState> _state = new(ProjectionState.Registering);

        readonly ConcurrentDictionary<ProjectionResultStoreConfigurationId, EventLogSequenceNumber> _positions = new();
        readonly ReplaySubject<IReadOnlyDictionary<ProjectionResultStoreConfigurationId, EventLogSequenceNumber>> _observablePositions = new(1);

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
        public IProjection Projection { get; }

        /// <inheritdoc/>
        public IProjectionEventProvider EventProvider { get; }

        /// <inheritdoc/>
        public IEnumerable<IProjectionResultStore> ResultStores => _resultStores.Values;

        /// <inheritdoc/>
        public IObservable<ProjectionState> State => _state;

        /// <inheritdoc/>
        public ProjectionState CurrentState => _state.Value;

        /// <inheritdoc/>
        public IObservable<IReadOnlyDictionary<ProjectionResultStoreConfigurationId, EventLogSequenceNumber>> Positions => _observablePositions;

        /// <inheritdoc/>
        public IReadOnlyDictionary<ProjectionResultStoreConfigurationId, EventLogSequenceNumber> CurrentPositions => new ReadOnlyDictionary<ProjectionResultStoreConfigurationId, EventLogSequenceNumber>(_positions);

        /// <inheritdoc/>
        public async Task Start()
        {
            foreach (var (configurationId, resultStore) in _resultStores)
            {
                await StartForConfigurationAndResultStore(configurationId, resultStore, _rewindsPerConfiguration.ContainsKey(configurationId));
            }
        }

        /// <inheritdoc/>
        public Task Pause()
        {
            _logger.Pausing(Projection.Identifier);
            _state.OnNext(ProjectionState.Paused);
            foreach (var (configurationId, _) in _resultStores)
            {
                StopForConfiguration(configurationId);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task Resume()
        {
            _logger.Resuming(Projection.Identifier);
            foreach (var (configurationId, resultStore) in _resultStores)
            {
                await StartForConfigurationAndResultStore(configurationId, resultStore, true);
            }
        }

        /// <inheritdoc/>
        public async Task Rewind()
        {
            _logger.Rewinding(Projection.Identifier);
            _state.OnNext(ProjectionState.Rewinding);
            foreach (var (configurationId, _) in _resultStores)
            {
                await Rewind(configurationId);
            }
        }

        /// <inheritdoc/>
        public async Task Rewind(ProjectionResultStoreConfigurationId configurationId)
        {
            _logger.RewindingForConfiguration(Projection.Identifier, configurationId);
            _rewindsPerConfiguration[configurationId] = true;

            if (CurrentState != ProjectionState.Rewinding)
            {
                _state.OnNext(ProjectionState.PartialRewinding);
            }

            if (CurrentState != ProjectionState.Registering)
            {
                var resultStore = _resultStores[configurationId];
                await StartForConfigurationAndResultStore(configurationId, resultStore, true);
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

        Task StartForConfigurationAndResultStore(ProjectionResultStoreConfigurationId configurationId, IProjectionResultStore resultStore, bool rewind)
        {
            var startTaskCompletionSource = new TaskCompletionSource();
            StopForConfiguration(configurationId);

            var cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSourcePerConfiguration[configurationId] = cancellationTokenSource;

            Task.Run(async () =>
            {
                startTaskCompletionSource.SetResult();
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

                    if (_catchUpPerConfiguration.IsEmpty)
                    {
                        _state.OnNext(ProjectionState.Active);
                    }
                }
                catch (Exception ex)
                {
                    _logger.ErrorStartingProviding(Projection.Identifier, ex);
                    _state.OnNext(ProjectionState.Failed);
                }
            }, cancellationTokenSource.Token);

            return startTaskCompletionSource.Task;
        }

        async Task<IProjectionResultStoreRewindScope> PrepareRewind(ProjectionResultStoreConfigurationId configurationId)
        {
            _state.OnNext(ProjectionState.Rewinding);
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
            _catchUpPerConfiguration[configurationId] = true;
            var offset = await _projectionPositions.GetFor(Projection, configurationId);
            if (offset == 0)
            {
                await resultStore.PrepareInitialRun(Projection.Model);
            }
            UpdatePositionFor(configurationId, offset);

            var exhausted = false;
            _state.OnNext(ProjectionState.CatchingUp);

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
                    }
                }
                if (!cursor.Current.Any()) exhausted = true;
            }

            _catchUpPerConfiguration.Remove(configurationId, out _);
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
            UpdatePositionFor(configurationId, nextSequenceNumber);
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

        void UpdatePositionFor(ProjectionResultStoreConfigurationId configurationId, EventLogSequenceNumber offset)
        {
            _positions[configurationId] = offset;
            _observablePositions.OnNext(new ReadOnlyDictionary<ProjectionResultStoreConfigurationId, EventLogSequenceNumber>(_positions));
        }
    }
}
