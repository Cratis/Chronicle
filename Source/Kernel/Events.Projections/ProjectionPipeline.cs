// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionPipeline"/>.
    /// </summary>
    public class ProjectionPipeline : IProjectionPipeline
    {
        readonly List<IProjectionStorage> _storageProviders = new();
        readonly ILogger<ProjectionPipeline> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="IProjectionPipeline"/>.
        /// </summary>
        /// <param name="eventProvider"><see cref="IProjectionEventProvider"/> to use.</param>
        /// <param name="projection">The <see cref="IProjection"/> the pipeline is for.</param>
        /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
        public ProjectionPipeline(IProjectionEventProvider eventProvider, IProjection projection, ILogger<ProjectionPipeline> logger)
        {
            EventProvider = eventProvider;
            Projection = projection;
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
                var tasks = _storageProviders.Select(storage => Task.Run(async () =>
                {
                    var keyResolver = Projection.GetKeyResolverFor(@event.Type);
                    var key = keyResolver(@event);
                    _logger.GettingInitialValues(@event.SequenceNumber);
                    var initialState = await storage.FindOrDefault(Projection.Model, key);
                    _logger.Projecting(@event.SequenceNumber);
                    var changeset = Projection.OnNext(@event, initialState);
                    _logger.SavingResult(@event.SequenceNumber);
                    await storage.ApplyChanges(Projection.Model, key, changeset);
                }));

                Task.WaitAll(tasks.ToArray());
            });
        }

        /// <inheritdoc/>
        public void Pause() => EventProvider.Pause(Projection);

        /// <inheritdoc/>
        public void Resume() => EventProvider.Resume(Projection);

        /// <inheritdoc/>
        public void StoreIn(IProjectionStorage storageProvider) => _storageProviders.Add(storageProvider);
    }
}
