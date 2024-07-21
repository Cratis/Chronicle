// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Cratis.Chronicle.Aggregates;

namespace Cratis.Chronicle.Aggregates;

public class StatefulAggregateRoot : AggregateRoot<StateForAggregateRoot>
{
    public int OnActivateCount;

    protected override Task OnActivate()
    {
        OnActivateCount++;
        return Task.CompletedTask;
    }
}
