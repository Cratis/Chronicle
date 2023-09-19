// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Kernel.MongoDB;
using Aksio.Cratis.Observation;
using Benchmark.Model;

namespace Benchmarks.Reducers;

public class ClientReducerJob : BenchmarkJob
{
    protected IObserver Observer { get; private set; } = null!;

    [IterationSetup]
    public void CleanEventStore()
    {
        SetExecutionContext();

        Database?.DropCollection(CollectionNames.Observers);
    }

    protected override void Setup()
    {
        SetExecutionContext();
        base.Setup();

        var key = new ObserverKey(GlobalVariables.MicroserviceId, GlobalVariables.TenantId, GlobalVariables.ObserverEventSequence);
        Observer = GrainFactory.GetGrain<IObserver>(Guid.Parse(CartReducer.Identifier), key);
    }
}
