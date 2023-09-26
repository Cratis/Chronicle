// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs;

/// <summary>
/// Represents a job for catching up an observer.
/// </summary>
public class CatchUpJob : Job
{
    readonly IObserverKeyIndexes _indexes;

    public CatchUpJob(IObserverKeyIndexes indexes)
    {
        _indexes = indexes;
    }


    protected override async Task Start()
    {
        var index = await _indexes.GetFor();
        var keys = await index.GetKeys();

        // For each key, create a job step
    }
}


public record CatchUpJobRequest(MicroserviceId MicroserviceId, TenantId TenantId, ObserverId ObserverId, EventSequenceId EventSequenceId);

public class CatchUpJobStep : IJobStep
{

}
