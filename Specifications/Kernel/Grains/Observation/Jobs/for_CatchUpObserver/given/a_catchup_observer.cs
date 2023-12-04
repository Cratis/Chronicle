// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.Cratis.Kernel.Keys;
using Orleans.Core;
using Orleans.TestKit;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs.for_CatchUpObserver.given;

public class a_catchup_observer : Specification
{
    protected TestKitSilo silo = new();
    protected Mock<IObserverKeyIndexes> indexes;
    protected CatchUpObserverWrapper job;
    protected JobKey job_key = new(MicroserviceId.Unspecified, TenantId.NotSet);
    protected JobId job_id => Guid.Parse("6341bcdb-c644-40ab-81cf-43907c510285");
    protected IStorage<CatchUpObserverState> state_storage;
    protected CatchUpObserverState state => state_storage.State;

    async Task Establish()
    {
        indexes = new();
        state_storage = silo.StorageManager.GetStorage<CatchUpObserverState>();

        silo.AddService(indexes.Object);
        silo.AddService(new JsonSerializerOptions());

        silo.AddProbe<ICatchUpObserver>(_ =>
        {
            return job;
        });

        job = await silo.CreateGrainAsync<CatchUpObserverWrapper>(job_id, job_key);
    }
}
