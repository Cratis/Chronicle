// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateRowComparer;

/// <summary>
/// Builds fully populated, equal-by-value rows for the comparer specs.
/// </summary>
public static class ComparerRows
{
    public static ObserverState Create() => new()
    {
        Id = new ObserverId("observer"),
        Owner = "owner",
        PartitionClaims = "claims",
        ObserverWideClaim = "wide",
        OwnershipRevision = 3,
        RunningStateChangedAt = new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
        TypeSetControl = "control",
        Initialization = "init",
        DeliveryFences = "fences",
        AlertLifecycleId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        AlertRevision = 4,
        AlertDisposition = AlertDisposition.Active,
        QuarantineEpisodeId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        WebhookProbe = "probe",
        LastHandledEventSequenceNumber = 5,
        NextEventSequenceNumber = 6,
        TailEventSequenceNumber = 7,
        RunningState = ObserverRunningState.Active,
        ReplayingPartitions = new HashSet<string> { "a" },
        CatchingUpPartitions = new HashSet<string> { "b" },
        FailedPartitions = [CreateFailedPartition()],
        FailedPartitionCount = 1,
        IsReplaying = false,
        SubscribesToAllEvents = false,
        HandledEventCount = 8,
        HandledEventCountPerEventType = new Dictionary<string, ulong> { ["type"] = 8 }
    };

    public static FailedPartition CreateFailedPartition() => new(
        "failed-partition",
        "partition",
        "observer",
        [CreateAttempt()],
        false)
    {
        IsQuarantined = false,
        WebhookDisposition = WebhookFailureDisposition.Retryable,
        WebhookRecoveryId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
        AttemptsBeforeBudgetReset = 1
    };

    public static FailedPartitionAttempt CreateAttempt() => new(
        new DateTimeOffset(2026, 5, 1, 9, 0, 0, TimeSpan.Zero),
        9,
        ["message"],
        "stack")
    {
        Kind = FailureKind.Unknown,
        DeliveryFence = new DeliveryFenceContext(new DateTimeOffset(2026, 5, 1, 11, 0, 0, TimeSpan.Zero), [new DeliveryFenceTarget("type", "key", "connection", "silo")])
    };
}
