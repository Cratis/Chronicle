// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Queries;
using Cratis.Extensions.MongoDB;
using MongoDB.Driver;
using Aksio.Queries.MongoDB;

namespace Cratis.Compliance.Read.Microservices
{
    [Route("/api/compliance/microservices")]
    public class Microservices : Controller
    {
        readonly IMongoCollection<Microservice> _collection;

        public Microservices(IMongoDBClientFactory mongoDBClientFactory)
        {
            var client = mongoDBClientFactory.Create(new MongoUrl("mongodb://localhost:27017"));
            var database = client.GetDatabase("read-models");
            _collection = database.GetCollection<Microservice>();
        }

        [HttpGet]
        public Task<ClientObservable<IEnumerable<Microservice>>> AllMicroservices() => _collection.Observe();
    }
}
