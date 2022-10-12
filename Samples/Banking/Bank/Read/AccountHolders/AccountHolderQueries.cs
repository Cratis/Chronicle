// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Read.AccountHolders;

[Route("/api/accountholders")]
public class AccountHolderQueries : Controller
{
    readonly IMongoCollection<AccountHolder> _collection;
    readonly IMongoCollection<AccountHolderWithAccounts> _withAccountsCollection;

    public AccountHolderQueries(
        IMongoCollection<AccountHolder> collection,
        IMongoCollection<AccountHolderWithAccounts> withAccountsCollection)
    {
        _collection = collection;
        _withAccountsCollection = withAccountsCollection;
    }

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

    [HttpGet("with-accounts")]
    public IEnumerable<AccountHolderWithAccounts> AllAccountHoldersWithAccounts() => new AccountHolderWithAccounts[] {
        new AccountHolderWithAccounts("Blah", "Blah", "123123", new("Somewhere", "3230", "Sandefjord", "Norway"), new IAccount[] {
            new DebitAccount("b4e4b614-f7c3-4815-afe5-9b57fc46df0f", "Debit", AccountType.Debit),
            new CreditAccount("029d7dff-133d-4193-a079-edd06a343b22", "Credit", AccountType.Credit),
        })
    };
}
