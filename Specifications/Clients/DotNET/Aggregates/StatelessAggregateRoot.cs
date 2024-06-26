// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates;

public class StatelessAggregateRoot : AggregateRoot
{
    public FirstEventType FirstEventTypeInstance;
    public SecondEventType SecondEventTypeInstance;

    public void OnFirstEvent(FirstEventType @event)
    {
        FirstEventTypeInstance = @event;
    }

    public void OnSecondEvent(SecondEventType @event, EventContext context)
    {
        SecondEventTypeInstance = @event;
    }

    public int OnActivateCount;

    protected override Task OnActivate()
    {
        OnActivateCount++;
        return Task.CompletedTask;
    }
}
