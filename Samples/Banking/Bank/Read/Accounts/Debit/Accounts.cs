// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts;
using Concepts.Accounts;

namespace Read.Accounts.Debit;

[Route("/api/accounts/debit")]
public class Accounts : Controller
{
    readonly IMongoCollection<DebitAccount> _accountsCollection;
    readonly IMongoCollection<DebitAccountLatestTransactions> _latestTransactionsCollection;

    public Accounts(
        IMongoCollection<DebitAccount> accountsCollection,
        IMongoCollection<DebitAccountLatestTransactions> latestTransactionsCollections)
    {
        _accountsCollection = accountsCollection;
        _latestTransactionsCollection = latestTransactionsCollections;
    }

    [HttpGet]
    public DebitAccount[] AllAccounts()
    {
        return new DebitAccount[]
        {
            new(new AccountId(Guid.Empty), "My Account", new PersonId(Guid.Empty), 0)
        };
    }

    [HttpGet("starting-with")]
    public async Task<IEnumerable<DebitAccount>> StartingWith([FromQuery] string? filter)
    {
        var filterDocument = Builders<DebitAccount>
            .Filter
            .Regex("name", $"^{filter ?? string.Empty}.*");

        var result = await _accountsCollection.FindAsync(filterDocument);
        return result.ToList();
    }

    [HttpGet("latest-transactions/{accountId}")]
    public DebitAccountLatestTransactions LatestTransactions([FromRoute] Guid accountId)
    {
        var items = _latestTransactionsCollection.Find(_ => _.Id == accountId).ToList();
        if (items.Count == 0) return null!;
        return items[0];
    }
}
