// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Queries;
using MongoDB.Driver;
using Aksio.Queries.MongoDB;

namespace Cratis.Compliance.Read.Microservices
{
    [Route("/api/compliance/microservices")]
    public class Microservices : Controller
    {
        readonly IMongoCollection<Microservice> _collection;

        public Microservices(IMongoCollection<Microservice> collection) => _collection = collection;

        [HttpGet]
        public Task<ClientObservable<IEnumerable<Microservice>>> AllMicroservices() => _collection.Observe();
    }
}
