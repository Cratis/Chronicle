// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.Accounts;

namespace Read.Accounts.Debit;

[Route("/api/accounts/debit")]
public class Accounts : Controller
{
    readonly IMongoCollection<DebitAccount> _accountsCollection;
    readonly IMongoCollection<DebitAccountLatestTransactions> _latestTransactionsCollection;
    readonly IMongoCollection<DebitAccountsPerDay> _accountsPerDayCollection;

    public Accounts(
        IMongoCollection<DebitAccount> accountsCollection,
        IMongoCollection<DebitAccountLatestTransactions> latestTransactionsCollections,
        IMongoCollection<DebitAccountsPerDay> accountsPerDayCollection)
    {
        _accountsCollection = accountsCollection;
        _latestTransactionsCollection = latestTransactionsCollections;
        _accountsPerDayCollection = accountsPerDayCollection;
    }

    [HttpGet]
    public Task<ClientObservable<IEnumerable<DebitAccount>>> AllAccounts()
    {
        return _accountsCollection.Observe();
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
    public DebitAccountLatestTransactions LatestTransactions([FromRoute] AccountId accountId)
    {
        var items = _latestTransactionsCollection.Find(_ => _.Id == accountId).ToList();
        if (items.Count == 0) return null!;
        return items[0];
    }

    [HttpGet("accounts-per-day")]
    public async Task<IEnumerable<DebitAccountsPerDay>> PerDay()
    {
        var result = await _accountsPerDayCollection.FindAsync(_ => true);
        return result.ToEnumerable();
    }
}
