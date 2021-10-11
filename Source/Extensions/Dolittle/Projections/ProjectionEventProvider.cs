// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Events.Projections;
using Cratis.Extensions.MongoDB;
using MongoDB.Driver;

namespace Cratis.Extensions.Dolittle.Projections
{
    public class ProjectionEventProvider : IProjectionEventProvider
    {
        readonly IMongoClient _client;
        readonly IMongoDatabase _database;

        public ProjectionEventProvider(IMongoDBClientFactory mongoDBClientFactory)
        {
            _client = mongoDBClientFactory.Create(MongoUrl.Create("mongodb://localhost:27017"));
            _database = _client.GetDatabase("event_store");
        }

        public IObservable<Event> ProvideFor(IProjection projection)
        {
            var subject = new ReplaySubject<Event>();
            return subject;
        }

        public void Pause(IProjection projection) => throw new NotImplementedException();
        public void Resume(IProjection projection) => throw new NotImplementedException();
        public Task Rewind(IProjection projection) => throw new NotImplementedException();
    }
}
