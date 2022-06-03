// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Events.Public.Accounts.Debit;

namespace Public.Accounts.Debit;

[Observer("2b110250-319d-4246-8b30-7a191b513e33", inbox: true)]
public class AccountBalanceObserver
{
    public Task Balance(AccountBalance @event, EventContext context)
    {
        Console.WriteLine($"Balance for {context.EventSourceId} : {@event.Balance}");
        return Task.CompletedTask;
    }
}
