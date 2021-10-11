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
    public class ProjectionEventProvider : IProjectionEventProvider
    {
        readonly IMongoClient _client;
        readonly IMongoDatabase _database;
        readonly IMongoCollection<EventStore.Event> _eventLogCollection;

        public ProjectionEventProvider(IMongoDBClientFactory mongoDBClientFactory)
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
        }

        public IObservable<Event> ProvideFor(IProjection projection)
        {
            var subject = new ReplaySubject<Event>();
            var events = _eventLogCollection.Find(_ => true).ToList();
            foreach (var @event in events)
            {
                var eventType = new EventType(@event.Metadata.TypeId.ToString());
                if (!projection.EventTypes.Any(_ => _ == eventType))
                {
                    continue;
                }

                var content = BsonSerializer.Deserialize<ExpandoObject>(@event.Content);
                subject.OnNext(
                    new Event(
                        @event.Id,
                        eventType,
                        @event.Metadata.Occurred,
                        @event.Metadata.EventSource.ToString(),
                        content));
            }
            return subject;
        }

        public void Pause(IProjection projection) => throw new NotImplementedException();
        public void Resume(IProjection projection) => throw new NotImplementedException();
        public Task Rewind(IProjection projection) => throw new NotImplementedException();
    }
}
