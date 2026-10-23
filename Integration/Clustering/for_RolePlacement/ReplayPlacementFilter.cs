// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;

using Orleans.Runtime.Placement;
using IObserver = KernelCore::Cratis.Chronicle.Observation.IObserver;
using IProjection = KernelCore::Cratis.Chronicle.Projections.IProjection;
using IProjectionObserverSubscriber = KernelCore::Cratis.Chronicle.Projections.IProjectionObserverSubscriber;
using ObserverSubscriberKey = KernelConcepts::Cratis.Chronicle.Concepts.Observation.ObserverSubscriberKey;
using ProjectionObserverSubscriber = KernelCore::Cratis.Chronicle.Projections.ProjectionObserverSubscriber;

namespace Cratis.Chronicle.Integration.Clustering.for_RolePlacement;

public class ReplayPlacementFilter(ReplayPlacementSignal signal, ILocalSiloDetails localSilo)
    : IOutgoingGrainCallFilter, IIncomingGrainCallFilter
{
    public async Task Invoke(IOutgoingGrainCallContext context)
    {
        if (signal.Capturing && IsStep(context))
        {
            await InvokeStepOnJobsSilo(context);
            return;
        }

        await context.Invoke();
        if (IsPartitionDeactivation(context)) signal.Deactivated.TrySetResult(context.SourceId!.Value);
    }

    public async Task Invoke(IIncomingGrainCallContext context)
    {
        if (IsStep(context))
        {
            RequestContext.Remove(IPlacementDirector.PlacementHintKey);
            if (signal.Capturing) signal.Steps[context.TargetId] = localSilo.SiloAddress;
        }

        await context.Invoke();
        RecordDelivery(context);
        if (IsPartitionRelease(context)) signal.Released.TrySetResult(context.TargetId);
        if (IsPartitionCompletion(context)) signal.PartitionCompleted.TrySetResult(true);
    }

    async Task InvokeStepOnJobsSilo(IOutgoingGrainCallContext context)
    {
        var previous = RequestContext.Get(IPlacementDirector.PlacementHintKey);
        try
        {
            // Only the real job step is forced to B. The incoming filter removes the hint before executing
            // it, so a subscriber can reach B only through its own placement strategy.
            RequestContext.Set(IPlacementDirector.PlacementHintKey, signal.JobsSilo);
            await context.Invoke();
        }
        finally
        {
            RequestContext.Remove(IPlacementDirector.PlacementHintKey);
            if (previous is not null) RequestContext.Set(IPlacementDirector.PlacementHintKey, previous);
        }
    }

    void RecordDelivery(IIncomingGrainCallContext context)
    {
        if (!signal.Capturing || context.MethodName != "OnNext") return;
        if (context.ImplementationMethod.DeclaringType != typeof(ProjectionObserverSubscriber)) return;
        var key = ObserverSubscriberKey.Parse(context.TargetId.Key.ToString());
        if (key.ObserverId.Value != signal.ProjectionId) return;
        signal.Deliveries[key.EventSourceId.Value] = context.TargetId;
    }

    bool IsStep(IGrainCallContext context) =>
        !string.IsNullOrEmpty(signal.StepType) && context.TargetId.Type.ToString().Contains(signal.StepType, StringComparison.OrdinalIgnoreCase);

    bool IsPartitionRelease(IGrainCallContext context) =>
        context.MethodName == nameof(IProjectionObserverSubscriber.ReleaseAfterPartition) && IsCapturedPartition(context.TargetId);

    bool IsPartitionDeactivation(IGrainCallContext context) =>
        context.MethodName == nameof(IProjection.UnsubscribeDefinitionsChanged) && context.SourceId is { } source && IsCapturedPartition(source);

    bool IsPartitionCompletion(IGrainCallContext context) =>
        context.MethodName == nameof(IObserver.PartitionReplayed) && IsCapturedPartition(context.TargetId, context.Request.GetArgument(0)?.ToString());

    bool IsCapturedPartition(GrainId grain, string? partition = null)
    {
        if (!signal.Capturing || signal.Partition is not { } expected) return false;
        if (!grain.ToString().Contains(signal.ProjectionId, StringComparison.Ordinal)) return false;
        return (partition ?? grain.ToString()).Contains(expected, StringComparison.Ordinal);
    }
}
