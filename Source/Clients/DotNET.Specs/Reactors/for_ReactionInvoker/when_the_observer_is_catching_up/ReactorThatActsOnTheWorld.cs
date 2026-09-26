// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_the_observer_is_catching_up;

/// <summary>
/// The shape the issue describes: a handler that pokes something outside the process, for which an event from
/// three days ago is history rather than an instruction to act.
/// </summary>
public class ReactorThatActsOnTheWorld : IReactor
{
    public int NotificationsSent { get; private set; }

    public int RecordsUpdated { get; private set; }

    [SkipCatchUp]
    public Task NotifySomeone(MyEvent @event, EventContext context)
    {
        NotificationsSent++;
        return Task.CompletedTask;
    }

    public Task UpdateSomething(MyOtherEvent @event, EventContext context)
    {
        RecordsUpdated++;
        return Task.CompletedTask;
    }
}

#pragma warning disable SA1402 // File may only contain a single type

[EventType]
public record MyOtherEvent;
