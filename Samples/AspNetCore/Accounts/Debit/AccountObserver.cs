// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Observation;

namespace Sample.Accounts.Debit
{
    [Observer("4680f4dc-5731-4fde-9b3c-a0f59b7713d9")]
    public class AccountObserver
    {
        public Task DebitAccountOpened(DebitAccountOpened @event)
        {
            //throw new ArgumentException("Not interested in this event");

            return Task.CompletedTask;
        }
    }
}
