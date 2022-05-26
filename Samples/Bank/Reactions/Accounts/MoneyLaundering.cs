// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Events.Accounts.Debit;

namespace Reactions.Accounts;

#pragma warning disable IDE0060

[Observer("6f71990b-8a96-4bf0-92ad-76f31cd7819f")]
public class MoneyLaundering
{
    readonly IImmediateProjections _immediateProjections;

    public MoneyLaundering(IImmediateProjections immediateProjections)
    {
        _immediateProjections = immediateProjections;
    }

    public async Task AccountOpened(DebitAccountOpened @event, EventContext context)
    {
        var count = await _immediateProjections.GetInstanceById<AccountsCounter>(context.EventSourceId);
        if (count?.Count > 42)
        {
            // Notify someone
        }
    }
}
