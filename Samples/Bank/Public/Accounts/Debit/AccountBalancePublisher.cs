// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Events.Accounts.Debit;
using Events.Public.Accounts.Debit;

namespace Public.Accounts.Debit;

[Observer("3d21838c-5a75-4fb7-bc7e-6c06f000eebe")]
public class AccountBalancePublisher
{
    readonly IImmediateProjections _immediateProjections;
    readonly IEventOutbox _eventOutbox;

    public AccountBalancePublisher(
        IImmediateProjections immediateProjections,
        IEventOutbox eventOutbox)
    {
        _immediateProjections = immediateProjections;
        _eventOutbox = eventOutbox;
    }

    public async Task Withdrawn(WithdrawalFromDebitAccountPerformed @event, EventContext context)
    {
        var account = await _immediateProjections.GetInstanceById<Account>(context.EventSourceId);
        await _eventOutbox.Append(context.EventSourceId, new AccountBalance(account.Balance));
    }

    public async Task Deposited(DepositToDebitAccountPerformed @event, EventContext context)
    {
        var account = await _immediateProjections.GetInstanceById<Account>(context.EventSourceId);
        await _eventOutbox.Append(context.EventSourceId, new AccountBalance(account.Balance));
    }
}
