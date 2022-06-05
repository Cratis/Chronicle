// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Events.Bank.Accounts.Debit;

namespace Public.Accounts.Debit;

[Observer("292a21dc-71de-4042-a313-4bcd45f6e0cb", inbox: true)]
public class AccountBalanceObserver
{
    public Task Balance(AccountBalance @event, EventContext context)
    {
        Console.WriteLine($"Balance for {context.EventSourceId} : {@event.Balance}");
        return Task.CompletedTask;
    }
}
