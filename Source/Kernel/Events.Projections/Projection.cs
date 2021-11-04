// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Events.Projections.Changes;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents the implementation of <see cref="IProjection"/>.
    /// </summary>
    public class Projection : IProjection
    {
        readonly ISubject<EventContext> _subject = new Subject<EventContext>();
        readonly IDictionary<EventType, EventValueProvider> _eventTypesToKeyResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="Projection"/> class.
        /// </summary>
        /// <param name="identifier">The unique identifier of the projection.</param>
        /// <param name="model">The target <see cref="Model"/>.</param>
        /// <param name="eventTypesWithKeyResolver">Collection of <see cref="EventTypeWithKeyResolver">event types with key resolvers</see> the projection should care about.</param>
        /// <param name="childProjections">Collection of <see cref="IProjection">child projections</see>, if any.</param>
        public Projection(
            ProjectionId identifier,
            Model model,
            IEnumerable<EventTypeWithKeyResolver> eventTypesWithKeyResolver,
            IEnumerable<IProjection> childProjections)
        {
            Identifier = identifier;
            Model = model;
            EventTypes = eventTypesWithKeyResolver.Select(_ => _.EventType);
            Event = FilterEventTypes(_subject);
            _eventTypesToKeyResolver = eventTypesWithKeyResolver.ToDictionary(_ => _.EventType, _ => _.KeyResolver);
            ChildProjections = childProjections;
        }

        /// <inheritdoc/>
        public IObservable<EventContext>  FilterEventTypes(IObservable<EventContext> observable) => observable.Where(_ => EventTypes.Any(et => et == _.Event.Type));

        /// <inheritdoc/>
        public ProjectionId Identifier { get; }

        /// <inheritdoc/>
        public Model Model { get; }

        /// <inheritdoc/>
        public IObservable<EventContext> Event { get; }

        /// <inheritdoc/>
        public IEnumerable<EventType> EventTypes { get; }

        /// <inheritdoc/>
        public IEnumerable<IProjection> ChildProjections { get; }

        /// <inheritdoc/>
        public void OnNext(Event @event, Changeset changeset)
        {
            var context = new EventContext(@event, changeset);
            _subject.OnNext(context);
        }

        /// <inheritdoc/>
        public EventValueProvider GetKeyResolverFor(EventType eventType)
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
