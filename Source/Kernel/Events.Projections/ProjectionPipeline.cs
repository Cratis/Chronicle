// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        readonly List<IProjectionStorage> _storageProviders = new();
        readonly IChangesetStorage _changesetStorage;
        readonly ILogger<ProjectionPipeline> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="IProjectionPipeline"/>.
        /// </summary>
        /// <param name="eventProvider"><see cref="IProjectionEventProvider"/> to use.</param>
        /// <param name="projection">The <see cref="IProjection"/> the pipeline is for.</param>
        /// <param name="changesetStorage"><see cref="IChangesetStorage"/> for storing changesets as they occur.</param>
        /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
        public ProjectionPipeline(
            IProjectionEventProvider eventProvider,
            IProjection projection,
            IChangesetStorage changesetStorage,
            ILogger<ProjectionPipeline> logger)
        {
            EventProvider = eventProvider;
            Projection = projection;
            _changesetStorage = changesetStorage;
            _logger = logger;
        }

        /// <inheritdoc/>
        public IProjectionEventProvider EventProvider { get; }

        /// <inheritdoc/>
        public IProjection Projection { get; }

        /// <inheritdoc/>
        public IEnumerable<IProjectionStorage> StorageProviders => _storageProviders;

        /// <inheritdoc/>
        public void Start()
        {
            var observable = EventProvider.ProvideFor(Projection);
            observable.Subscribe(@event =>
            {
                _logger.HandlingEvent(@event.SequenceNumber);
                var correlationId = CorrelationId.New();
                var changesets = new List<Changeset>();
                var tasks = _storageProviders.Select(storage => Task.Run(async () => await HandleEventFor(Projection, storage, @event, changesets)));
                Task.WaitAll(tasks.ToArray());
                _changesetStorage.Save(correlationId, changesets).Wait();
            });
        }

        /// <inheritdoc/>
        public void Pause() => EventProvider.Pause(Projection);

        /// <inheritdoc/>
        public void Resume() => EventProvider.Resume(Projection);

        /// <inheritdoc/>
        public void StoreIn(IProjectionStorage storageProvider) => _storageProviders.Add(storageProvider);

        async Task HandleEventFor(IProjection projection, IProjectionStorage storage, Event @event, List<Changeset> changesets)
        {
            var keyResolver = projection.GetKeyResolverFor(@event.Type);
            var key = keyResolver(@event);
            _logger.GettingInitialValues(@event.SequenceNumber);
            var initialState = await storage.FindOrDefault(Projection.Model, key);
            var changeset = new Changeset(@event, initialState);
            changesets.Add(changeset);
            _logger.Projecting(@event.SequenceNumber);
            projection.OnNext(@event, changeset);
            _logger.SavingResult(@event.SequenceNumber);
            await storage.ApplyChanges(Projection.Model, key, changeset);

            foreach (var child in projection.ChildProjections)
            {
                await HandleEventFor(child, storage, @event, changesets);
            }
        }
    }
}
