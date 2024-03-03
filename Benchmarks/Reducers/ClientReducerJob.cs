// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Benchmark.Model;
using Cratis.Kernel.Grains.Observation;
using Cratis.Kernel.Storage.MongoDB;
using Cratis.Observation;

namespace Benchmarks.Reducers;

public class ClientReducerJob : BenchmarkJob
{
    protected IObserver Observer { get; private set; } = null!;

    [IterationSetup]
    public void CleanEventStore()
    {
        Database?.DropCollection(WellKnownCollectionNames.Observers);
    }

    protected override void Setup()
    {
        base.Setup();

        var key = new ObserverKey(GlobalVariables.MicroserviceId, GlobalVariables.TenantId, GlobalVariables.ObserverEventSequence);
        Observer = GrainFactory.GetGrain<IObserver>(Guid.Parse(CartReducer.Identifier), key);
    }
}
