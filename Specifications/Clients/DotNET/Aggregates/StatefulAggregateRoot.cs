// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Aksio.Cratis.Aggregates;

public class StatefulAggregateRoot : AggregateRoot<StateForAggregateRoot>
{
    public int OnActivateCount;

    protected override Task OnActivate()
    {
        OnActivateCount++;
        return Task.CompletedTask;
    }
}
