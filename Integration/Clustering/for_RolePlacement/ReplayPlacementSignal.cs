// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Chronicle.Integration.Clustering.for_RolePlacement;

public class ReplayPlacementSignal
{
    public SiloAddress JobsSilo { get; set; } = SiloAddress.Zero;
    public bool Capturing { get; set; }
    public string ProjectionId { get; private set; } = string.Empty;
    public string StepType { get; private set; } = string.Empty;
    public string? Partition { get; private set; }
    public TaskCompletionSource<GrainId> Released { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource<GrainId> Deactivated { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource<bool> PartitionCompleted { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public ConcurrentDictionary<GrainId, SiloAddress> Steps { get; } = new();
    public ConcurrentDictionary<GrainId, SiloAddress> Subscribers { get; } = new();
    public ConcurrentDictionary<string, GrainId> Deliveries { get; } = new();

    public void BeginCapture(SiloAddress jobsSilo, Type stepInterface, string projectionId, string? partition = null)
    {
        JobsSilo = jobsSilo;
        ProjectionId = projectionId;
        StepType = stepInterface.Name[1..];
        Partition = partition;
        Steps.Clear();
        Subscribers.Clear();
        Deliveries.Clear();
        Released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Deactivated = new(TaskCreationOptions.RunContinuationsAsynchronously);
        PartitionCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Capturing = true;
    }
}
