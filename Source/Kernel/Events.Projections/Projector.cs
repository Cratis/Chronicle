// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents the implementation of <see cref="IProjector"/>.
    /// </summary>
    public class Projection : IProjector
    {
        readonly ISubject<EventContext> _subject = new Subject<EventContext>();
        public IObservable<EventContext> Event { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Projection"/> class.
        /// </summary>
        /// <param name="eventTypes">Collection of <see cref="EventType">event types</see> the projection should care about.</param>
        public Projection(IEnumerable<EventType> eventTypes)
        {
            Event = _subject.Where(_ => eventTypes.Any(et => et == _.Event.Type));
        }

        /// <inheritdoc/>
        public async Task<Changeset> OnNext(Event @event, IProjectionStorage storage)
        {
            var initialState = await storage.FindOrDefault("");
            var context = new EventContext(@event, new Changeset(initialState));
            _subject.OnNext(context);
            return context.Changeset;
        }
    }
}
