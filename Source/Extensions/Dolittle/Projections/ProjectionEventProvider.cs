// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Dynamic;
using System.Reactive.Subjects;
using Cratis.Events.Projections;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using IEventStore = Cratis.Extensions.Dolittle.EventStore.IEventStore;
using IEventStream = Cratis.Extensions.Dolittle.EventStore.IEventStream;

namespace Cratis.Extensions.Dolittle.Projections
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionEventProvider"/> for the Dolittle event store.
    /// </summary>
    public class ProjectionEventProvider : IProjectionEventProvider
    {
        readonly IEventStore _eventStore;
        readonly IEventStream _eventStream;
        readonly IProjectionPositions _projectionPositions;
        readonly ILogger<ProjectionEventProvider> _logger;
        readonly ConcurrentDictionary<IProjection, ReplaySubject<Event>> _projectionsWithSubject = new();
        readonly ConcurrentDictionary<IProjection, ReplaySubject<Event>> _pausedProjectionsWithSubject = new();

        /// <summary>
        /// Initializes a new instance of <see cref="ProjectionEventProvider"/>.
        /// </summary>
        /// <param name="eventStore">The Dolittle <see cref="IEventStore"/>.</param>
        /// <param name="projectionPositions"><see cref="IProjectionPositions"/> for maintaining positions.</param>
        /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
        public ProjectionEventProvider(
            IEventStore eventStore,
            IProjectionPositions projectionPositions,
            ILogger<ProjectionEventProvider> logger)
        {
            _eventStore = eventStore;
            _eventStream = eventStore.GetStream(EventStore.EventStreamId.EventLog);
            _projectionPositions = projectionPositions;
            _logger = logger;
        }

        /// <inheritdoc/>
        public IObservable<Event> ProvideFor(IProjection projection)
        {
            _logger.ProvidingFor(projection.Identifier);

            var subject = new ReplaySubject<Event>();
            Task.Run(() => StartProvidingFor(projection, subject));
            return subject;
        }

        /// <inheritdoc/>
        public void Pause(IProjection projection)
        {
            _logger.Pausing(projection.Identifier);

            if (_projectionsWithSubject.TryRemove(projection, out var subject))
            {
                _pausedProjectionsWithSubject.TryAdd(projection, subject);
            }
        }

        /// <inheritdoc/>
        public void Resume(IProjection projection)
        {
            _logger.Resuming(projection.Identifier);

            if (_pausedProjectionsWithSubject.TryRemove(projection, out var subject))
            {
                _projectionsWithSubject.TryAdd(projection, subject);
            }
        }

        /// <inheritdoc/>
        public async Task Rewind(IProjection projection)
        {
            _logger.Rewinding(projection.Identifier);

            await _projectionPositions.Reset(projection);
            await CatchUp(projection, _projectionsWithSubject[projection]);
        }

        void WatchForEvents()
        {
            Task.Run(async () =>
            {
                var cursor = _eventStream.Watch();
                while (cursor.MoveNext())
                {
                    if (!cursor.Current.Any()) continue;

                    foreach (var (projection, subject) in _projectionsWithSubject)
                    {
                        await OnNext(projection, subject, cursor.Current.Select(_ => _.FullDocument));
                    }
                }
            });
        }

        async Task StartProvidingFor(IProjection projection, ReplaySubject<Event> subject)
        {
            try
            {
                if (!projection.EventTypes.Any())
                {
                    _logger.SkippingProvidingForProjectionDueToNoEventTypes(projection.Identifier);
                    await Task.CompletedTask;
                    return;
                }

                await CatchUp(projection, subject);
                var tail = _eventStream.Count();
                await _projectionPositions.Save(projection, (uint)tail);
                _projectionsWithSubject.TryAdd(projection, subject);
                WatchForEvents();
            }
            catch (Exception ex)
            {
                _logger.ErrorStartingProviding(projection.Identifier, ex);
            }
        }

        async Task CatchUp(IProjection projection, ReplaySubject<Event> subject)
        {
            _logger.CatchingUp(projection.Identifier);
            var offset = await _projectionPositions.GetFor(projection);
            var eventTypes = projection.EventTypes.Select(_ => new global::Dolittle.SDK.Events.EventType(Guid.Parse(_.Value))).ToArray();

            var exhausted = false;

            while (!exhausted)
            {
                var cursor = await _eventStream.GetFromPosition(offset.Value, eventTypes);
                while (await cursor.MoveNextAsync())
                {
                    if (!cursor.Current.Any())
                    {
                        exhausted = true;
                        break;
                    }

                    offset = await OnNext(projection, subject, cursor.Current);
                }
            }
        }

        async Task<EventLogSequenceNumber> OnNext(IProjection projection, ReplaySubject<Event> subject, IEnumerable<EventStore.Event> events)
        {
            EventLogSequenceNumber lastSavedPosition = 0;
            foreach (var @event in events)
            {
                var eventType = new EventType(@event.Metadata.TypeId.ToString());
                if (projection.EventTypes.Any(_ => _ == eventType))
                {
                    var content = BsonSerializer.Deserialize<ExpandoObject>(@event.Content);
                    _logger.ProvidingEvent(@event.Id);
                    subject.OnNext(
                        new Event(
                            @event.Id,
                            eventType,
                            @event.Metadata.Occurred,
                            @event.Metadata.EventSource,
                            content));
                }

                await _projectionPositions.Save(projection, @event.Id + 1);
                lastSavedPosition = @event.Id + 1;
            }

            return lastSavedPosition;
        }
    }
}
