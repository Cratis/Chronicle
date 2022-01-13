// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Queries;
using Aksio.Queries.MongoDB;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Cratis.Compliance.Read.GDPR
{
    [Route("/api/compliance/gdpr/people")]
    public class People : Controller
    {
        readonly IMongoCollection<Person> _collection;

        public People(IMongoCollection<Person> collection) => _collection = collection;

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
