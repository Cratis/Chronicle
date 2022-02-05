// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections
{
    /// <summary>
    /// Represents the implementation of <see cref="IProjection"/>.
    /// </summary>
    public class Projection : IProjection
    {
        readonly ISubject<ProjectionEventContext> _subject = new Subject<ProjectionEventContext>();
        readonly IDictionary<EventType, ValueProvider<AppendedEvent>> _eventTypesToKeyResolver;

        /// <inheritdoc/>
        public ProjectionId Identifier { get; }

        /// <inheritdoc/>
        public ProjectionName Name { get; }

        /// <inheritdoc/>
        public ProjectionPath Path { get; }

        /// <inheritdoc/>
        public Model Model { get; }

        /// <inheritdoc/>
        public bool IsPassive { get; }

        /// <inheritdoc/>
        public bool IsRewindable { get; }

        /// <inheritdoc/>
        public IObservable<ProjectionEventContext> Event { get; }

        /// <inheritdoc/>
        public IEnumerable<EventType> EventTypes { get; }

        /// <inheritdoc/>
        public IEnumerable<IProjection> ChildProjections { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Projection"/> class.
        /// </summary>
        /// <param name="identifier">The unique identifier of the projection.</param>
        /// <param name="name">The name of the projection.</param>
        /// <param name="path">The qualified path of the projection.</param>
        /// <param name="model">The target <see cref="Model"/>.</param>
        /// <param name="passive">Whether or not the projection is a passive projection.</param>
        /// <param name="rewindable">Whether or not the projection is rewindable.</param>
        /// <param name="eventTypesWithKeyResolver">Collection of <see cref="EventTypeWithKeyResolver">event types with key resolvers</see> the projection should care about.</param>
        /// <param name="childProjections">Collection of <see cref="IProjection">child projections</see>, if any.</param>
        public Projection(
            ProjectionId identifier,
            ProjectionName name,
            ProjectionPath path,
            Model model,
            bool passive,
            bool rewindable,
            IEnumerable<EventTypeWithKeyResolver> eventTypesWithKeyResolver,
            IEnumerable<IProjection> childProjections)
        {
            Identifier = identifier;
            Name = name;
            Model = model;
            IsPassive = passive;
            IsRewindable = rewindable;
            EventTypes = eventTypesWithKeyResolver.Select(_ => _.EventType);
            Event = FilterEventTypes(_subject);
            Path = path;
            _eventTypesToKeyResolver = eventTypesWithKeyResolver.ToDictionary(_ => _.EventType, _ => _.KeyResolver);
            ChildProjections = childProjections;
        }

        /// <inheritdoc/>
        public IObservable<ProjectionEventContext> FilterEventTypes(IObservable<ProjectionEventContext> observable) => observable.Where(_ => EventTypes.Any(et => et == _.Event.Metadata.Type));

        /// <inheritdoc/>
        public IObservable<AppendedEvent> FilterEventTypes(IObservable<AppendedEvent> observable) => observable.Where(_ => EventTypes.Any(et => et == _.Metadata.Type));

        /// <inheritdoc/>
        public void OnNext(ProjectionEventContext eventContext)
        {
            _subject.OnNext(eventContext);
        }

        /// <inheritdoc/>
        public bool Accepts(EventType eventType) => _eventTypesToKeyResolver.ContainsKey(eventType);

        /// <inheritdoc/>
        public ValueProvider<AppendedEvent> GetKeyResolverFor(EventType eventType)
        {
            ThrowIfMissingKeyResolverForEventType(eventType);
            return _eventTypesToKeyResolver[eventType];
        }

        void ThrowIfMissingKeyResolverForEventType(EventType eventType)
        {
            if (!_eventTypesToKeyResolver.ContainsKey(eventType))
            {
                throw new MissingKeyResolverForEventType(eventType);
            }
        }
    }
}
