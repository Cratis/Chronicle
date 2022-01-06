// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Queries;
using Aksio.Queries.MongoDB;
using Cratis.Extensions.MongoDB;
using MongoDB.Driver;

namespace Cratis.Compliance.Read.GDPR
{
    [Route("/api/compliance/gdpr/people")]
    public class People : Controller
    {
        readonly IMongoCollection<Person> _collection;

        public People(IMongoDBClientFactory mongoDBClientFactory)
        {
            var client = mongoDBClientFactory.Create(new MongoUrl("mongodb://localhost:27017"));
            var database = client.GetDatabase("read-models");
            _collection = database.GetCollection<Person>();
        }

        [HttpGet]
        public Task<ClientObservable<IEnumerable<Person>>> AllPeople() => _collection.Observe();

        [HttpGet("search")]
        public async Task<IEnumerable<Person>> SearchForPeople([FromQuery] string query)
        {
            var filter = Builders<Person>.Filter.Text(query, new TextSearchOptions { CaseSensitive = false, DiacriticSensitive = false });
            var result = await _collection.FindAsync(filter);
            return result.ToList();
        }
    }
}
