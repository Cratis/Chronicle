// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using Concepts.Customers;
using Events.AccountHolders;
using Events.Accounts.Credit;
using Events.Accounts.Debit;

namespace Read.AccountHolders;

[Observer("e1eb9022-a4b7-47c6-ba80-c7c610497ee8")]
public class AccountHolderWithAccountsObserver
{
    readonly IMongoCollection<AccountHolderWithAccounts> _collection;
    readonly IImmediateProjections _immediateProjections;

    public AccountHolderWithAccountsObserver(IMongoCollection<AccountHolderWithAccounts> collection, IImmediateProjections immediateProjections)
    {
        _collection = collection;
        _immediateProjections = immediateProjections;
    }

    public Task AccountHolderRegistered(AccountHolderRegistered @event, EventContext context)
    {
        var CustomerId = GetCustomerIdFromString(context.EventSourceId);
        var model = new AccountHolderWithAccounts(CustomerId, @event.FirstName, @event.LastName, new());
        return _collection.ReplaceOneAsync(_ => _.Id == CustomerId, model, new ReplaceOptions { IsUpsert = true });
    }

    public async Task CreditAccountOpened(CreditAccountOpened @event, EventContext context)
    {
        var CustomerId = @event.Owner;
        var personalInformation = await _immediateProjections.GetInstanceById<AccountHolderPersonalInformation>(CustomerId.Value);
        var result = await _collection.FindAsync(_ => _.Id == @event.Owner);
        var model = result.FirstOrDefault() ?? new(CustomerId, personalInformation.FirstName, personalInformation.LastName, new Collection<IAccount>());
        model.Accounts.Add(new CreditAccount(@context.EventSourceId, @event.Name, AccountType.Credit));
        await _collection.ReplaceOneAsync(_ => _.Id == CustomerId, model, new ReplaceOptions { IsUpsert = true });
    }

    public async Task DebitAccountOpened(DebitAccountOpened @event, EventContext context)
    {
        var CustomerId = @event.Owner;
        var personalInformation = await _immediateProjections.GetInstanceById<AccountHolderPersonalInformation>(CustomerId.Value);
        var result = await _collection.FindAsync(_ => _.Id == @event.Owner);
        var model = result.FirstOrDefault() ?? new(CustomerId, personalInformation.FirstName, personalInformation.LastName, new Collection<IAccount>());
        model.Accounts.Add(new DebitAccount(@context.EventSourceId, @event.Name, AccountType.Debit));
        await _collection.ReplaceOneAsync(_ => _.Id == CustomerId, model, new ReplaceOptions { IsUpsert = true });
    }

#pragma warning disable CA5351
    CustomerId GetCustomerIdFromString(string input)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        return new CustomerId(new Guid(hash));
    }
}
