// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Events.Accounts.Debit;

namespace Read.Accounts.Debit;

[Observer("b195adfb-e743-4457-b295-5637368436e3")]
public class DebitAccountObserver
{
    public Task Opened(DebitAccountOpened @event)
    {
        Console.WriteLine(@event);
        return Task.CompletedTask;
    }
}
