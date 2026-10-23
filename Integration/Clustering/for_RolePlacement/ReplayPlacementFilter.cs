// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using Orleans.Runtime.Placement;
using IHandleEventsForPartition = KernelCore::Cratis.Chronicle.Observation.Jobs.IHandleEventsForPartition;
using IProjectionObserverSubscriber = KernelCore::Cratis.Chronicle.Projections.IProjectionObserverSubscriber;

namespace Cratis.Chronicle.Integration.Clustering.for_RolePlacement;

public class ReplayPlacementFilter(ReplayPlacementSignal signal, ILocalSiloDetails localSilo, IGrainFactory grains)
    : IOutgoingGrainCallFilter, IIncomingGrainCallFilter
{
    public async Task Invoke(IOutgoingGrainCallContext context)
    {
        if (!signal.Capturing || !IsPartitionStep(context))
        {
            await context.Invoke();
            return;
        }

        var previous = RequestContext.Get(IPlacementDirector.PlacementHintKey);
        try
        {
            // Only the real partition job step is forced to B. The incoming filter removes the hint before
            // executing it, so a subscriber can reach B only through its own placement strategy.
            RequestContext.Set(IPlacementDirector.PlacementHintKey, signal.JobsSilo);
            await context.Invoke();
        }
        finally
        {
            RequestContext.Remove(IPlacementDirector.PlacementHintKey);
            if (previous is not null) RequestContext.Set(IPlacementDirector.PlacementHintKey, previous);
        }
    }

    public async Task Invoke(IIncomingGrainCallContext context)
    {
        if (IsPartitionStep(context))
        {
            RequestContext.Remove(IPlacementDirector.PlacementHintKey);
            if (signal.Capturing) signal.Steps[context.TargetId] = localSilo.SiloAddress;
        }

        if (signal.Capturing && IsProjectionRelease(context))
        {
            // Capture the exact subscriber while its final job-step turn is still active. After Invoke it
            // deactivates, so querying only after replay completion would count zero and prove nothing.
            var statistics = await grains.GetGrain<IManagementGrain>(0).GetDetailedGrainStatistics();
            signal.Subscribers[context.TargetId] = statistics.Single(statistic => statistic.GrainId == context.TargetId).SiloAddress;
        }

        await context.Invoke();
    }

    static bool IsProjectionRelease(IGrainCallContext context) =>
        context.MethodName == nameof(IProjectionObserverSubscriber.ReleaseAfterPartition) &&
        context.TargetId.ToString().Contains(ReplayPlacementSignal.ProjectionId, StringComparison.Ordinal);

    static bool IsPartitionStep(IGrainCallContext context) =>
        context.TargetId.Type.ToString().Contains(nameof(IHandleEventsForPartition)[1..], StringComparison.OrdinalIgnoreCase);
}
