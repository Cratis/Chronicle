// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.Cratis.Kernel.Keys;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs.for_CatchUpObserver.given;

public class a_catchup_observer : GrainSpecification<CatchUpObserverState>
{
    protected Mock<IObserverKeyIndexes> indexes;
    protected CatchUpObserverWrapper job;
    protected JobKey job_key = new(MicroserviceId.Unspecified, TenantId.NotSet);

    protected override Guid GrainId => Guid.Parse("6341bcdb-c644-40ab-81cf-43907c510285");
    protected override string GrainKeyExtension => job_key;

    protected override Grain GetGrainInstance()
    {
        indexes = new();

        job = new(indexes.Object);
        job = new CatchUpObserverWrapper(indexes.Object);
        return job;
    }
}
