// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers;

/// <summary>
/// Converter methods for working with <see cref="Observation.ObserverState"/> converting to and from SQL representations.
/// </summary>
public static class ObserverStateConverters
{
    /// <summary>
    /// Convert to a <see cref="ObserverState">SQL</see> representation.
    /// </summary>
    /// <param name="definition"><see cref="Concepts.Observation.Reactors.ReactorDefinition"/> to convert.</param>
    /// <returns>Converted <see cref="ObserverState"/>.</returns>
    public static ObserverState ToSql(this Observation.ObserverState definition) =>
        new()
        {
            Id = definition.Identifier,
            Owner = definition.Owner?.Value,
            TypeSetControl = ObserverTypeSetControlJson.Serialize(definition.TypeSetControl),
            DeliveryFences = definition.DeliveryFences is null ? null : JsonSerializer.Serialize(definition.DeliveryFences, ObserverStateJson.Options),
            Initialization = definition.Initialization is null ? null : JsonSerializer.Serialize(definition.Initialization, ObserverStateJson.Options),
            AlertLifecycleId = definition.AlertLifecycleId,
            AlertRevision = definition.AlertRevision,
            AlertDisposition = definition.AlertDisposition,
            QuarantineEpisodeId = definition.QuarantineEpisodeId,
            WebhookProbe = definition.WebhookProbe is null ? null : JsonSerializer.Serialize(definition.WebhookProbe, ObserverStateJson.Options),
            LastHandledEventSequenceNumber = definition.LastHandledEventSequenceNumber,
            NextEventSequenceNumber = definition.NextEventSequenceNumber,
            TailEventSequenceNumber = definition.TailEventSequenceNumber,
            RunningState = definition.RunningState,
            ReplayingPartitions = definition.ReplayingPartitions.Select(partition => partition.Value.ToString()!).ToHashSet(),
            CatchingUpPartitions = definition.CatchingUpPartitions.Select(partition => partition.Value.ToString()!).ToHashSet(),
            FailedPartitions = definition.FailedPartitions.Select(fp => new FailedPartition(
                fp.Id.Value.ToString(),
                fp.Partition.Value?.ToString() ?? string.Empty,
                fp.ObserverId.Value,
                fp.Attempts.Select(a => new FailedPartitionAttempt(
                    a.Occurred,
                    a.SequenceNumber.Value,
                    a.Messages,
                    a.StackTrace)
                {
                    Kind = a.Kind,
                    DeliveryFence = a.DeliveryFence
                }),
                fp.IsResolved)
            {
                IsQuarantined = fp.IsQuarantined,
                WebhookDisposition = fp.WebhookDisposition,
                WebhookRecoveryId = fp.WebhookRecoveryId,
                AttemptsBeforeBudgetReset = fp.AttemptsBeforeBudgetReset,
                RecoveryCursor = fp.RecoveryCursor,
                RecoveryOwner = fp.RecoveryOwner,
                Hold = fp.Hold
            }).ToList(),
            FailedPartitionCount = definition.FailedPartitionCount,
            IsReplaying = definition.IsReplaying,
            SubscribesToAllEvents = definition.SubscribesToAllEvents,
            HandledEventCount = definition.HandledEventCount.Value,
            HandledEventCountPerEventType = definition.HandledEventCountPerEventType.ToDictionary(kvp => kvp.Key.Value, kvp => kvp.Value.Value)
        };

    /// <summary>
    /// Convert to <see cref="Observation.ObserverState"/> from <see cref="ObserverState"/>.
    /// </summary>
    /// <param name="state"><see cref="ObserverState"/> to convert from.</param>
    /// <returns>Converted <see cref="Observation.ObserverState"/>.</returns>
    public static Observation.ObserverState ToKernel(this ObserverState state) =>
        new(
            state.Id,
            state.LastHandledEventSequenceNumber,
            state.RunningState,
            state.ReplayingPartitions.Select(partition => (Key)partition).ToHashSet(),
            state.CatchingUpPartitions.Select(partition => (Key)partition).ToHashSet(),
            state.FailedPartitions.Select(fp => new Concepts.Observation.FailedPartition
            {
                Id = new(Guid.Parse(fp.Id)),
                Partition = new Key(fp.Partition, Properties.ArrayIndexers.NoIndexers),
                ObserverId = new(fp.ObserverId),
                Attempts = fp.Attempts.Select(a => new Concepts.Observation.FailedPartitionAttempt
                {
                    Occurred = a.Occurred,
                    SequenceNumber = a.SequenceNumber,
                    Messages = a.Messages,
                    StackTrace = a.StackTrace,
                    Kind = a.Kind,
                    DeliveryFence = a.DeliveryFence
                }),
                IsResolved = fp.IsResolved,
                IsQuarantined = fp.IsQuarantined,
                WebhookDisposition = fp.WebhookDisposition,
                WebhookRecoveryId = fp.WebhookRecoveryId,
                AttemptsBeforeBudgetReset = fp.AttemptsBeforeBudgetReset,
                RecoveryCursor = fp.RecoveryCursor,
                RecoveryOwner = fp.RecoveryOwner,
                Hold = fp.Hold
            }).ToList(),
            state.FailedPartitionCount,
            state.IsReplaying,
            state.SubscribesToAllEvents)
        {
            Owner = state.Owner is null ? null : new Concepts.EventSequences.EventSequenceId(state.Owner),
            TypeSetControl = ObserverTypeSetControlJson.Deserialize(state.TypeSetControl),
            DeliveryFences = state.DeliveryFences is null ? null : JsonSerializer.Deserialize<Observation.DeliveryFenceState[]>(state.DeliveryFences, ObserverStateJson.Options),
            Initialization = state.Initialization is null ? null : JsonSerializer.Deserialize<Observation.ObserverInitialization>(state.Initialization, ObserverStateJson.Options),
            AlertLifecycleId = state.AlertLifecycleId,
            AlertRevision = state.AlertRevision,
            AlertDisposition = state.AlertDisposition,
            QuarantineEpisodeId = state.QuarantineEpisodeId,
            WebhookProbe = state.WebhookProbe is null ? null : JsonSerializer.Deserialize<Observation.WebhookProbeState>(state.WebhookProbe, ObserverStateJson.Options),
            NextEventSequenceNumber = state.NextEventSequenceNumber,
            TailEventSequenceNumber = state.TailEventSequenceNumber,
            HandledEventCount = state.HandledEventCount,
            HandledEventCountPerEventType = state.HandledEventCountPerEventType?.ToDictionary(kvp => (Concepts.Events.EventTypeId)kvp.Key, kvp => (Concepts.Events.EventCount)kvp.Value) ?? new Dictionary<Concepts.Events.EventTypeId, Concepts.Events.EventCount>()
        };
}
