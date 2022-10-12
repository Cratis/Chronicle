// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Concepts;
using Events.AccountHolders;
using Events.Accounts.Credit;
using Events.Accounts.Debit;

namespace Read.AccountHolders;

[Observer("e1eb9022-a4b7-47c6-ba80-c7c610497ee8")]
public class AccountHolderWithAccountsObserver
{
    readonly IMongoCollection<AccountHolderWithAccounts> _collection;

    public AccountHolderWithAccountsObserver(IMongoCollection<AccountHolderWithAccounts> collection)
    {
        _collection = collection;
    }

    public Task AccountHolderRegistered(AccountHolderRegistered @event, EventContext context)
    {
        var personId = GetPersonIdFromString(context.EventSourceId);
        var model = new AccountHolderWithAccounts(personId, @event.FirstName, @event.LastName, new());
        return _collection.ReplaceOneAsync(_ => _.Id == personId, model, new ReplaceOptions { IsUpsert = true });
    }

    public async Task CreditAccountOpened(CreditAccountOpened @event, EventContext context)
    {
        var personId = @event.Owner;
        var result = await _collection.FindAsync(_ => _.Id == @event.Owner);
        var model = result.First();
        model.Accounts.Add(new CreditAccount(@context.EventSourceId, @event.Name, AccountType.Credit));
        await _collection.ReplaceOneAsync(_ => _.Id == personId, model, new ReplaceOptions { IsUpsert = true });
    }

    public async Task DebitAccountOpened(DebitAccountOpened @event, EventContext context)
    {
        var personId = @event.Owner;
        var result = await _collection.FindAsync(_ => _.Id == @event.Owner);
        var model = result.First();
        model.Accounts.Add(new DebitAccount(@context.EventSourceId, @event.Name, AccountType.Debit));
        await _collection.ReplaceOneAsync(_ => _.Id == personId, model, new ReplaceOptions { IsUpsert = true });
    }

#pragma warning disable CA5351
    PersonId GetPersonIdFromString(string input)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        return new PersonId(new Guid(hash));
    }
}
