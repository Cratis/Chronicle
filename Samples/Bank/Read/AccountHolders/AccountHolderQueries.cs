// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Read.AccountHolders
{
    [Route("/api/accountholders")]
    public class AccountHolderQueries : Controller
    {
        readonly IMongoCollection<AccountHolder> _collection;

        public AccountHolderQueries(IMongoCollection<AccountHolder> collection) => _collection = collection;

        [HttpGet]
        public IEnumerable<AccountHolder> AllAccountHolders() => _collection.Find(_ => true).ToList();

        [HttpGet("starting-with")]
        public async Task<IEnumerable<AccountHolder>> AccountHoldersStartingWith([FromQuery] string? filter)
        {
            var filterDocument = Builders<AccountHolder>
                .Filter
                .Regex("firstName", $"^{filter ?? string.Empty}.*");

            var result = await _collection.FindAsync(filterDocument);
            return result.ToList();
        }
    }
}
