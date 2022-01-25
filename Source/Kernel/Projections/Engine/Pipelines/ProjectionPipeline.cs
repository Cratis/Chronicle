// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Aksio.Cratis.Events.Projections.Pipelines.JobSteps;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Reactive;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.Projections.Pipelines
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionPipeline"/>.
    /// </summary>
    public class ProjectionPipeline : IProjectionPipeline
    {
        readonly ConcurrentDictionary<ProjectionResultStoreConfigurationId, IProjectionResultStore> _resultStores = new();
        readonly ConcurrentDictionary<ProjectionResultStoreConfigurationId, ISubject<AppendedEvent>> _subjectsPerConfiguration = new();
        readonly ConcurrentDictionary<ProjectionResultStoreConfigurationId, IDisposable> _subscriptionsPerConfiguration = new();
        readonly IProjectionPipelineHandler _handler;
        readonly IProjectionPipelineJobs _pipelineJobs;
        readonly ILogger<ProjectionPipeline> _logger;
        readonly BehaviorSubject<ProjectionState> _state = new(ProjectionState.Registering);
        readonly ObservableCollection<IProjectionPipelineJob> _jobs = new();
        readonly BehaviorSubject<ProjectionPipelineStatus> _status = new(ProjectionPipelineStatus.Initial);

        /// <inheritdoc/>
        public IProjection Projection { get; }

        /// <inheritdoc/>
        public IProjectionEventProvider EventProvider { get; }

        /// <inheritdoc/>
        public IDictionary<ProjectionResultStoreConfigurationId, IProjectionResultStore> ResultStores => _resultStores;

        /// <inheritdoc/>
        public IObservable<ProjectionState> State => _state;

        /// <inheritdoc/>
        public IObservable<IReadOnlyDictionary<ProjectionResultStoreConfigurationId, EventLogSequenceNumber>> Positions => _handler.Positions;

        /// <inheritdoc/>
        public ProjectionState CurrentState => _state.Value;

        /// <inheritdoc/>
        public IObservableCollection<IProjectionPipelineJob> Jobs => _jobs;

        /// <inheritdoc/>
        public IObservable<ProjectionPipelineStatus> Status => _status;

        /// <summary>
        /// Initializes a new instance of the <see cref="IProjectionPipeline"/>.
        /// </summary>
        /// <param name="projection">The <see cref="IProjection"/> the pipeline is for.</param>
        /// <param name="eventProvider"><see cref="IProjectionEventProvider"/> to use.</param>
        /// <param name="handler"><see cref="IProjectionPipelineHandler"/> to use.</param>
        /// <param name="jobs"><see cref="IProjectionPipelineJobs"/> for creating jobs.</param>
        /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
        public ProjectionPipeline(
            IProjection projection,
            IProjectionEventProvider eventProvider,
            IProjectionPipelineHandler handler,
            IProjectionPipelineJobs jobs,
            ILogger<ProjectionPipeline> logger)
        {
            EventProvider = eventProvider;
            _handler = handler;
            _pipelineJobs = jobs;
            Projection = projection;
            _logger = logger;

            if (projection.IsPassive)
            {
                _state.OnNext(ProjectionState.Passive);
            }

            _status.OnNext(new(projection, _status.Value.State, _status.Value.Positions, _status.Value.Jobs));
            _state.Subscribe(_ => _status.OnNext(new(projection, _, _status.Value.Positions, _status.Value.Jobs)));
            _handler.Positions.Subscribe(_ => _status.OnNext(new(projection, _status.Value.State, _.ToDictionary(_ => _.Key, _ => _.Value), _status.Value.Jobs)));
            _jobs.Subscribe(_ => _status.OnNext(new(projection, _status.Value.State, _status.Value.Positions, _)));
        }

        /// <inheritdoc/>
        public async Task Start()
        {
            if (Projection.IsPassive)
            {
                _logger.IgnoringOperationForPassive(Projection.Identifier, "Start");
                return;
            }

            _logger.Starting(Projection.Identifier);
            _state.OnNext(ProjectionState.CatchingUp);
            await AddAndRunJobs(_pipelineJobs.Catchup(this));
            _state.OnNext(ProjectionState.Active);

            await SetupHandling();
        }

        /// <inheritdoc/>
        public async Task Pause()
        {
            if (Projection.IsPassive)
            {
                _logger.IgnoringOperationForPassive(Projection.Identifier, "Pause");
                return;
            }

            _logger.Pausing(Projection.Identifier);

            await StopAllJobs();
            foreach (var (_, subscription) in _subscriptionsPerConfiguration)
            {
                subscription.Dispose();
            }
            await EventProvider.StopProvidingFor(this);
            _subscriptionsPerConfiguration.Clear();
            _state.OnNext(ProjectionState.Paused);
        }

        /// <inheritdoc/>
        public async Task Resume()
        {
            if (Projection.IsPassive)
            {
                _logger.IgnoringOperationForPassive(Projection.Identifier, "Resume");
                return;
            }

            if (CurrentState == ProjectionState.Active ||
                CurrentState == ProjectionState.CatchingUp ||
                CurrentState == ProjectionState.Rewinding)
            {
                return;
            }

            _logger.Resuming(Projection.Identifier);
            _state.OnNext(ProjectionState.CatchingUp);
            await AddAndRunJobs(_pipelineJobs.Catchup(this));
            await SetupHandling();
            _state.OnNext(ProjectionState.Active);
        }

        /// <inheritdoc/>
        public async Task Rewind()
        {
            if (Projection.IsPassive)
            {
                _logger.IgnoringOperationForPassive(Projection.Identifier, "Rewind");
                return;
            }
            if (!Projection.IsRewindable)
            {
                _logger.IgnoringRewind(Projection.Identifier);
                return;
            }

            ThrowIfRewindAlreadyInProgress();

            _logger.Rewinding(Projection.Identifier);
            _state.OnNext(ProjectionState.Rewinding);
            await AddAndRunJobs(_pipelineJobs.Rewind(this));
            _state.OnNext(ProjectionState.Active);
        }

        /// <inheritdoc/>
        public async Task Rewind(ProjectionResultStoreConfigurationId configurationId)
        {
            if (Projection.IsPassive)
            {
                _logger.IgnoringOperationForPassive(Projection.Identifier, "Rewind");
                return;
            }
            if (!Projection.IsRewindable)
            {
                _logger.IgnoringRewind(Projection.Identifier);
                return;
            }

            ThrowIfRewindAlreadyInProgressForConfiguration(configurationId);

            _logger.RewindingForConfiguration(Projection.Identifier, configurationId);
            _state.OnNext(ProjectionState.Rewinding);
            await AddAndRunJobs(new[] { _pipelineJobs.Rewind(this, configurationId) });
            _state.OnNext(ProjectionState.Active);
        }

        /// <inheritdoc/>
        public async Task Suspend(string reason)
        {
            if (Projection.IsPassive)
            {
                _logger.IgnoringOperationForPassive(Projection.Identifier, "Rewind");
                return;
            }

            _logger.Suspended(Projection.Identifier, reason);
            await StopAllJobs();
            foreach (var (_, subscription) in _subscriptionsPerConfiguration)
            {
                subscription.Dispose();
            }
            await EventProvider.StopProvidingFor(this);
            _subscriptionsPerConfiguration.Clear();
            _state.OnNext(ProjectionState.Suspended);
        }

        /// <inheritdoc/>
        public void StoreIn(ProjectionResultStoreConfigurationId configurationId, IProjectionResultStore resultStore)
        {
            _resultStores[configurationId] = resultStore;
            _subjectsPerConfiguration[configurationId] = new ReplaySubject<AppendedEvent>();
            _handler.InitializeFor(this, configurationId);
        }

        async Task AddAndRunJobs(IEnumerable<IProjectionPipelineJob> jobs)
        {
            foreach (var job in jobs)
            {
                _jobs.Add(job);
            }

            foreach (var job in jobs)
            {
                await job.Run();
                _jobs.Remove(job);
            }
        }

        async Task StopAllJobs()
        {
            foreach (var job in _jobs)
            {
                await job.Stop();
            }
            _jobs.Clear();
        }

        async Task SetupHandling()
        {
            foreach (var (configurationId, subject) in _subjectsPerConfiguration)
            {
                var resultStore = _resultStores[configurationId];
                await EventProvider.ProvideFor(this, subject);

                if (_subscriptionsPerConfiguration.ContainsKey(configurationId))
                {
                    _subscriptionsPerConfiguration[configurationId].Dispose();
                    _subscriptionsPerConfiguration.Remove(configurationId, out _);
                }
                _subscriptionsPerConfiguration[configurationId] = Projection
                    .FilterEventTypes(subject)
                    .Subscribe(@event => _handler.Handle(@event, this, resultStore, configurationId).Wait());
            }
        }

        void ThrowIfRewindAlreadyInProgress()
        {
            if (_jobs.Any(_ => _.Name == ProjectionPipelineJobs.RewindJob))
            {
                throw new RewindAlreadyInProgress(this);
            }
        }

        void ThrowIfRewindAlreadyInProgressForConfiguration(ProjectionResultStoreConfigurationId configurationId)
        {
            var rewindJob = _jobs.FirstOrDefault(_ => _.Name == ProjectionPipelineJobs.RewindJob);
            if (rewindJob != default)
            {
                foreach (var step in rewindJob.Steps)
                {
                    if (step is Rewind rewind && rewind.ConfigurationId == configurationId)
                    {
                        throw new RewindAlreadyInProgressForConfiguration(this, configurationId);
                    }
                }
            }
        }
    }
}
