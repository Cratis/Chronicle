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
        readonly IProjectionPipelineHandler _handler;
        readonly IProjectionPipelineJobs _pipelineJobs;
        readonly ILogger<ProjectionPipeline> _logger;
        readonly BehaviorSubject<ProjectionState> _state = new(ProjectionState.Registering);
        readonly BehaviorSubject<IEnumerable<IProjectionPipelineJob>> _jobs = new(Array.Empty<IProjectionPipelineJob>());

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
        }

        /// <inheritdoc/>
        public IProjection Projection { get; }

        /// <inheritdoc/>
        public IProjectionEventProvider EventProvider { get; }

        /// <inheritdoc/>
        public IDictionary<ProjectionResultStoreConfigurationId, IProjectionResultStore> ResultStores => _resultStores;

        /// <inheritdoc/>
        public IObservable<ProjectionState> State => _state;

        /// <inheritdoc/>
        public ProjectionState CurrentState => _state.Value;

        /// <inheritdoc/>
        public IObservable<IReadOnlyDictionary<ProjectionResultStoreConfigurationId, EventLogSequenceNumber>> Positions => _handler.Positions;

        /// <inheritdoc/>
        public IObservable<IEnumerable<IProjectionPipelineJob>> Jobs => _jobs;

        /// <inheritdoc/>
        public async Task Start()
        {
            _logger.Starting(Projection.Identifier);
            _state.OnNext(ProjectionState.CatchingUp);
            await AddAndRunJobs(_pipelineJobs.Catchup(this));
            _state.OnNext(ProjectionState.Active);

            foreach (var (configurationId, subject) in _subjectsPerConfiguration)
            {
                var resultStore = _resultStores[configurationId];
                EventProvider.ProvideFor(this, subject);
                _subscriptionsPerConfiguration[configurationId] = Projection
                    .FilterEventTypes(subject)
                    .Subscribe(@event => _handler.Handle(@event, this, resultStore, configurationId).Wait());
            }
        }

        /// <inheritdoc/>
        public Task Pause()
        {
            _logger.Pausing(Projection.Identifier);
            _state.OnNext(ProjectionState.Paused);

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task Resume()
        {
            _logger.Resuming(Projection.Identifier);

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task Rewind()
        {
            _logger.Rewinding(Projection.Identifier);
            _state.OnNext(ProjectionState.Rewinding);
            await AddAndRunJobs(_pipelineJobs.Rewind(this));
            _state.OnNext(ProjectionState.Active);
        }

        /// <inheritdoc/>
        public async Task Rewind(ProjectionResultStoreConfigurationId configurationId)
        {
            _logger.RewindingForConfiguration(Projection.Identifier, configurationId);
            _state.OnNext(ProjectionState.Rewinding);
            await AddAndRunJobs(new[] { _pipelineJobs.Rewind(this, configurationId) });
            _state.OnNext(ProjectionState.Active);
        }

        /// <inheritdoc/>
        public async Task Suspend(string reason)
        {
            _logger.Suspended(Projection.Identifier, reason);
            foreach (var job in _jobs.Value)
            {
                await job.Stop();
            }
            _jobs.OnNext(Array.Empty<IProjectionPipelineJob>());
            _state.OnNext(ProjectionState.Suspended);
        }

        /// <inheritdoc/>
        public void StoreIn(ProjectionResultStoreConfigurationId configurationId, IProjectionResultStore resultStore)
        {
            _resultStores[configurationId] = resultStore;
            _subjectsPerConfiguration[configurationId] = new ReplaySubject<Event>();
            _handler.InitializeFor(this, configurationId);
        }

        async Task AddAndRunJobs(IEnumerable<IProjectionPipelineJob> jobs)
        {
            _jobs.OnNext(_jobs.Value.Concat(jobs));

            foreach (var job in jobs)
            {
                await job.Run();
                _jobs.OnNext(_jobs.Value.Where(_ => _ != job));
            }
        }
    }
}
