// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Builds the recovery cursors and failure contexts of a type-set controlled partition step.
/// </summary>
internal static class ControlledRecovery
{
    /// <summary>
    /// Gets the hold for the first eligible event that no target can take.
    /// </summary>
    /// <param name="first">The first uncovered event.</param>
    /// <param name="state">The step state.</param>
    /// <returns>The pending hold.</returns>
    internal static PendingTypeSetHold HoldAt(AppendedEvent first, HandleEventsForPartitionState state) =>
        new(first.Context.EventType.Id, CursorAt(first.Context.SequenceNumber, state));

    /// <summary>
    /// Gets the failure context that lets a transient failure keep the recovery episode and cursor.
    /// </summary>
    /// <param name="state">The step state.</param>
    /// <param name="failedAt">The position the failure happened at.</param>
    /// <returns>The context, including the cursor of an initial replay that has no failure episode yet.</returns>
    internal static PartitionFailureContext ContextFor(HandleEventsForPartitionState state, EventSequenceNumber failedAt) =>
        new()
        {
            EpisodeId = state.EpisodeId,
            DeferralId = state.DeferralId,
            RecoveryCursor = CursorAt(failedAt, state)
        };

    static FailedPartitionRecoveryCursor CursorAt(EventSequenceNumber position, HandleEventsForPartitionState state) =>
        state.EventObservationState == EventObservationState.Replay
            ? new(position, FailedPartitionRecoveryMode.Replay, state.EndEventSequenceNumber)
            : new(position, FailedPartitionRecoveryMode.Live, null);
}
