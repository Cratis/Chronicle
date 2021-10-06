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
        readonly List<IProjectionStorage> _storages = new();
        public IObservable<EventContext> Event { get; }

        public Projection(IEnumerable<EventType> eventTypes)
        {
            Event = _subject.Where(_ => eventTypes.Any(et => et == _.Event.Type));
        }

        /// <inheritdoc/>
        public async Task OnNext(Event @event)
        {
            // If no storage provided - throw an exception

            await Parallel.ForEachAsync(_storages, async (storage, _) =>
            {
                var initialState = await storage.FindOrDefault("");
                var context = new EventContext(@event, new Changeset(initialState));
                _subject.OnNext(context);
            });
        }

        /// <inheritdoc/>
        public void StoreIn(IProjectionStorage storage) => _storages.Add(storage);
    }
}
