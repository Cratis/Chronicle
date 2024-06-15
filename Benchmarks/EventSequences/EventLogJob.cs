// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.EventSequences;
using Microsoft.Extensions.DependencyInjection;
using IEventSequence = Cratis.Chronicle.Grains.EventSequences.IEventSequence;

namespace Benchmarks.EventSequences;

public abstract class EventLogJob : BenchmarkJob
{
    protected IEventSequence? EventSequence { get; private set; }

    [IterationSetup]
    public void CleanEventStore()
    {
        Database?.DropCollection(WellKnownCollectionNames.EventLog);
        Database?.DropCollection(WellKnownCollectionNames.EventSequences);
    }

    protected override void Setup()
    {
        base.Setup();

        var grainFactory = GlobalVariables.ServiceProvider.GetRequiredService<IGrainFactory>();
        EventSequence = grainFactory.GetGrain<IEventSequence>(EventSequenceId.Log, keyExtension: new EventSequenceKey(GlobalVariables.MicroserviceId, GlobalVariables.TenantId));
    }

    protected async Task Perform(Func<IEventSequence, Task> action)
    {
        await action(EventSequence!);
    }
}
