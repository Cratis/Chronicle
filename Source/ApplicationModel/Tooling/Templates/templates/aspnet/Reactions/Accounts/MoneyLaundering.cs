// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Events.Accounts.Debit;

namespace Reactions.Accounts;

#pragma warning disable IDE0060

[Observer("6f71990b-8a96-4bf0-92ad-76f31cd7819f")]
public class MoneyLaundering
{
    readonly IImmediateProjections _immediateProjections;
    readonly IEventLog _eventLog;

    public MoneyLaundering(IImmediateProjections immediateProjections, IEventLog eventLog)
    {
        _immediateProjections = immediateProjections;
        _eventLog = eventLog;
    }

    public async Task AccountOpened(DebitAccountOpened @event, EventContext context)
    {
        var count = await _immediateProjections.GetInstanceById<AccountsCounter>(context.EventSourceId);
        if (count.Count > 42)
        {
            Console.WriteLine("Hellu");
        }
        await _eventLog.Append(Guid.Empty.ToString(), new PossibleMoneyLaunderingDetected(@event.Owner, context.EventSourceId, DateOnly.FromDateTime(context.Occurred.Date)));
    }
}
