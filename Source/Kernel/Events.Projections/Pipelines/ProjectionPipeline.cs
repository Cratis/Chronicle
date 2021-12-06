// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;

namespace Cratis.Events.Projections.Pipelines
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
        readonly IProjectionPipelineHandler _handler;
        readonly ILogger<ProjectionPipeline> _logger;
        readonly BehaviorSubject<ProjectionState> _state = new(ProjectionState.Registering);

        /// <summary>
        /// Initializes a new instance of the <see cref="IProjectionPipeline"/>.
        /// </summary>
        /// <param name="projection">The <see cref="IProjection"/> the pipeline is for.</param>
        /// <param name="eventProvider"><see cref="IProjectionEventProvider"/> to use.</param>
        /// <param name="handler"><see cref="IProjectionPipelineHandler"/> to use.</param>
        /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
        public ProjectionPipeline(
            IProjection projection,
            IProjectionEventProvider eventProvider,
            IProjectionPipelineHandler handler,
            ILogger<ProjectionPipeline> logger)
        {
            EventProvider = eventProvider;
            _handler = handler;
            Projection = projection;
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
        public IObservable<IReadOnlyDictionary<ProjectionResultStoreConfigurationId, EventLogSequenceNumber>> Positions => _handler.Positions;

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
        public Task Suspend(string reason)
        {
            _logger.Suspended(Projection.Identifier, reason);
            _state.OnNext(ProjectionState.Suspended);
            return Task.CompletedTask;
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
                    EventProvider.ProvideFor(this, subject);
                    _subscriptionsPerConfiguration[configurationId] = Projection
                        .FilterEventTypes(subject)
                        .Subscribe(@event => _handler.Handle(@event, this, resultStore, configurationId).Wait());

                    if (_catchUpPerConfiguration.IsEmpty)
                    {
                        _state.OnNext(ProjectionState.Active);
                    }
                }
                catch (Exception ex)
                {
                    _logger.ErrorStartingProviding(Projection.Identifier, ex);
                    _state.OnNext(ProjectionState.Suspended);
                }
            }, cancellationTokenSource.Token);

            return startTaskCompletionSource.Task;
        }

        Task<IProjectionResultStoreRewindScope> PrepareRewind(ProjectionResultStoreConfigurationId configurationId)
        {
            var resultStore = _resultStores[configurationId];
            var scope = resultStore.BeginRewindFor(Projection.Model);
            //await _projectionPositions.Reset(Projection, configurationId);
            return Task.FromResult(scope);
        }

        async Task CatchUp(
            ProjectionResultStoreConfigurationId configurationId,
            IProjectionResultStore resultStore,
            CancellationToken cancellationToken)
        {
            _logger.CatchingUp(Projection.Identifier, configurationId);
            _catchUpPerConfiguration[configurationId] = true;
            var offset = 0U; //await _projectionPositions.GetFor(Projection, configurationId);
            if (offset == 0)
            {
                await resultStore.PrepareInitialRun(Projection.Model);
            }
            //UpdatePositionFor(configurationId, offset);

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
                        offset = await _handler.Handle(@event, this, resultStore, configurationId);
                    }
                }
                if (!cursor.Current.Any()) exhausted = true;
            }

            _catchUpPerConfiguration.Remove(configurationId, out _);
        }
    }
}
