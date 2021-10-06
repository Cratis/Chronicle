// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        public IObservable<EventContext> Event { get; }

        public Projection(IEnumerable<EventType> eventTypes)
        {
            Event = _subject.Where(_ => eventTypes.Any(et => et == _.Event.EventType));
        }

        /// <inheritdoc/>
        public void Next(Event @event)
        {
            var context = new EventContext(@event, new Changeset());

            _subject.OnNext(context);
        }
    }
}
