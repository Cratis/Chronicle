// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Dynamic;
using System.Reactive.Subjects;
using Cratis.Events.Projections;
using Cratis.Extensions.MongoDB;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Extensions.Dolittle.Projections
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionEventProvider"/> for the Dolittle event store.
    /// </summary>
    public class ProjectionEventProvider : IProjectionEventProvider
    {
        readonly IMongoClient _client;
        readonly IMongoDatabase _database;
        readonly IMongoCollection<EventStore.Event> _eventLogCollection;
        readonly IProjectionPositions _projectionPositions;
        readonly ILogger<ProjectionEventProvider> _logger;
        readonly ConcurrentDictionary<IProjection, ReplaySubject<Event>> _projectionsWithSubject = new();
        readonly ConcurrentDictionary<IProjection, ReplaySubject<Event>> _pausedProjectionsWithSubject = new();

        /// <summary>
        /// Initializes a new instance of <see cref="ProjectionEventProvider"/>.
        /// </summary>
        /// <param name="mongoDBClientFactory"><see cref="IMongoDBClientFactory"/> for connecting to MongoDB.</param>
        /// <param name="projectionPositions"><see cref="IProjectionPositions"/> for maintaining positions.</param>
        /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
        public ProjectionEventProvider(
            IMongoDBClientFactory mongoDBClientFactory,
            IProjectionPositions projectionPositions,
            ILogger<ProjectionEventProvider> logger)
        {
            var mongoUrlBuilder = new MongoUrlBuilder
            {
                Servers = new[] { new MongoServerAddress("localhost", 27017) }
            };
            var url = mongoUrlBuilder.ToMongoUrl();
            var settings = MongoClientSettings.FromUrl(url);
            _client = mongoDBClientFactory.Create(settings);
            _database = _client.GetDatabase("event_store");
            _eventLogCollection = _database.GetCollection<EventStore.Event>("event-log");
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
                var cursor = _eventLogCollection.Watch(options: new() { FullDocument = ChangeStreamFullDocumentOption.UpdateLookup });
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
                await CatchUp(projection, subject);
                var tail = _eventLogCollection.CountDocuments(FilterDefinition<EventStore.Event>.Empty);
                await _projectionPositions.Save(projection, (uint)tail);
                _projectionsWithSubject.TryAdd(projection, subject);
                WatchForEvents();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        async Task CatchUp(IProjection projection, ReplaySubject<Event> subject)
        {
            _logger.CatchingUp(projection.Identifier);
            var offset = await _projectionPositions.GetFor(projection);
            var eventTypeFilters = projection.EventTypes.Select(_ => Builders<EventStore.Event>.Filter.Eq(_ => _.Metadata.TypeId, Guid.Parse(_.Value))).ToArray();

            var exhausted = false;

            while (!exhausted)
            {
                var offsetFilter = Builders<EventStore.Event>.Filter.Gt(_ => _.Id, offset.Value);
                var filter = Builders<EventStore.Event>.Filter.And(
                    offsetFilter,
                    Builders<EventStore.Event>.Filter.Or(eventTypeFilters)
                );

                _logger.GettingEventsFromOffset(offset.Value);

                var cursor = await _eventLogCollection.FindAsync(
                    filter,
                    new()
                    {
                        Sort = Builders<EventStore.Event>.Sort.Ascending(_ => _.Id)
                    });

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
                            @event.Metadata.EventSource.ToString(),
                            content));
                }

                await _projectionPositions.Save(projection, @event.Id);
                lastSavedPosition = @event.Id;
            }

            return lastSavedPosition;
        }
    }
}
