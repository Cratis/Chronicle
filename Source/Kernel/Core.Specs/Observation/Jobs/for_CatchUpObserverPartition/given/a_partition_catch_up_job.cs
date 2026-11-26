// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserverPartition.given;

public class a_partition_catch_up_job : for_CatchUpObserver.given.a_catch_up_observer_job
{
    protected TestableCatchUpObserverPartition _partitionJob;

    protected override async Task CreateJob() => _partitionJob = await _silo.CreateGrainAsync<TestableCatchUpObserverPartition>(_jobId, _jobKey);
}
