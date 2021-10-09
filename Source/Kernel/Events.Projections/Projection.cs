// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents the implementation of <see cref="IProjection"/>.
    /// </summary>
    public class Projection : IProjection
    {
        readonly ISubject<EventContext> _subject = new Subject<EventContext>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Projection"/> class.
        /// </summary>
        /// <param name="identifier">The unique identifier of the projection.</param>
        /// <param name="model">The target <see cref="IModel"/>.</param>
        /// <param name="eventTypes">Collection of <see cref="EventType">event types</see> the projection should care about.</param>
        public Projection(ProjectionId identifier, IModel model, IEnumerable<EventType> eventTypes)
        {
            Identifier = identifier;
            Model = model;
            Event = _subject.Where(_ => eventTypes.Any(et => et == _.Event.Type));
        }

        /// <inheritdoc/>
        public ProjectionId Identifier { get; }

        /// <inheritdoc/>
        public IModel Model { get; }

        /// <inheritdoc/>
        public IObservable<EventContext> Event { get; }

        /// <inheritdoc/>
        public Changeset OnNext(Event @event, ExpandoObject initialState)
        {
            var context = new EventContext(@event, new Changeset(this, @event, initialState));
            _subject.OnNext(context);
            return context.Changeset;
        }
    }
}
