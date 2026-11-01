// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Integration.Clustering.for_JobsRolePlacement;

public class JobStepPlacementFilter(JobStepPlacementSignal signal, ILocalSiloDetails localSilo) : IIncomingGrainCallFilter
{
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        if (signal.Capturing && context.TargetContext.GrainInstance is IJobStep)
        {
            signal.Steps[context.TargetId] = localSilo.SiloAddress;
        }

        await context.Invoke();
    }
}
