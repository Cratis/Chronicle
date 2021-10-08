// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionPipeline"/>.
    /// </summary>
    public class ProjectionPipeline : IProjectionPipeline
    {
        readonly List<IProjectionStorage> _storageProviders = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="IProjectionPipeline"/>.
        /// </summary>
        /// <param name="eventProvider"><see cref="IProjectionEventProvider"/> to use.</param>
        /// <param name="projection">The <see cref="IProjection"/> the pipeline is for.</param>
        public ProjectionPipeline(IProjectionEventProvider eventProvider, IProjection projection)
        {
            EventProvider = eventProvider;
            Projection = projection;
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
                var storageProviders = _storageProviders.ToArray();
                Parallel.ForEach(storageProviders, (storage, _) =>
                {
                    Projection.OnNext(@event, storage).Wait();
                });
            });
        }

        /// <inheritdoc/>
        public void Pause() => EventProvider.Pause(Projection);

        /// <inheritdoc/>
        public void Resume() => EventProvider.Resume(Projection);

        /// <inheritdoc/>
        public void StoreIn(IProjectionStorage storage) => _storageProviders.Add(storage);
    }
}
