// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Subjects;
using Cratis.Events.Projections;
using Cratis.Extensions.MongoDB;
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

        /// <summary>
        /// Initializes a new instance of <see cref="ProjectionEventProvider"/>.
        /// </summary>
        /// <param name="mongoDBClientFactory"><see cref="IMongoDBClientFactory"/> for connecting to MongoDB.</param>
        /// <param name="projectionPositions"><see cref="IProjectionPositions"/> for maintaining positions.</param>
        public ProjectionEventProvider(
            IMongoDBClientFactory mongoDBClientFactory,
            IProjectionPositions projectionPositions)
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
        }

        /// <inheritdoc/>
        public IObservable<Event> ProvideFor(IProjection projection)
        {
            var subject = new ReplaySubject<Event>();
            Task.Run(() => StartProvidingFor(projection, subject));
            return subject;
        }

        /// <inheritdoc/>
        public void Pause(IProjection projection) => throw new NotImplementedException();

        /// <inheritdoc/>
        public void Resume(IProjection projection) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task Rewind(IProjection projection) => throw new NotImplementedException();

        async Task StartProvidingFor(IProjection projection, ReplaySubject<Event> subject)
        {
            try
            {
                var offset = await _projectionPositions.GetFor(projection);
                var offsetFilter = Builders<EventStore.Event>.Filter.Gt(_ => _.Id, offset.Value);
                var eventTypeFilters = projection.EventTypes.Select(_ => Builders<EventStore.Event>.Filter.Eq(_ => _.Metadata.TypeId, Guid.Parse(_.Value))).ToArray();
                var filter = Builders<EventStore.Event>.Filter.And(
                    offsetFilter,
                    Builders<EventStore.Event>.Filter.Or(eventTypeFilters)
                );

                var cursor = await _eventLogCollection.FindAsync(
                    filter,
                    new()
                    {
                        Sort = Builders<EventStore.Event>.Sort.Ascending(_ => _.Id)
                    });

                while (await cursor.MoveNextAsync())
                {
                    foreach (var @event in cursor.Current)
                    {
                        var eventType = new EventType(@event.Metadata.TypeId.ToString());
                        var content = BsonSerializer.Deserialize<ExpandoObject>(@event.Content);
                        subject.OnNext(
                            new Event(
                                @event.Id,
                                eventType,
                                @event.Metadata.Occurred,
                                @event.Metadata.EventSource.ToString(),
                                content));

                        await _projectionPositions.Save(projection, @event.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
