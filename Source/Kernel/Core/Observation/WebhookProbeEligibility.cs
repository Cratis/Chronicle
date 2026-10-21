// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Determines which durable webhook failures and claims may be probed.
/// </summary>
internal static class WebhookProbeEligibility
{
    /// <summary>
    /// Determines whether an unresolved failure permits automatic delivery.
    /// </summary>
    /// <param name="failure">The stored failure.</param>
    /// <returns>Whether automatic probing is permitted.</returns>
    internal static bool IsEligible(FailedPartition failure) =>
        !failure.IsResolved && HasEligibleDisposition(failure) && failure.Hold is null;

    /// <summary>
    /// Selects the claimed episode or the oldest eligible recovery cursor.
    /// </summary>
    /// <param name="failures">The observer failures.</param>
    /// <param name="control">The durable probe control.</param>
    /// <returns>The eligible candidate, if any.</returns>
    internal static FailedPartition? Select(IEnumerable<FailedPartition> failures, WebhookProbeState control) =>
        control.EpisodeId is { } episode
            ? failures.FirstOrDefault(_ => _.Id == episode && IsEligible(_))
            : failures.Where(IsEligible).OrderBy(_ => _.EffectiveRecoveryCursor.Position).ThenBy(_ => _.Id.Value).FirstOrDefault();

    /// <summary>
    /// Checks the claim cursor, including checkpointed successful progress.
    /// </summary>
    /// <param name="control">The durable claim.</param>
    /// <param name="failure">The current failure.</param>
    /// <returns>Whether the cursor still belongs to the claim.</returns>
    internal static bool MatchesPosition(WebhookProbeState control, FailedPartition failure)
    {
        if (control.Position is null) return true;
        if (failure.EffectiveRecoveryCursor.Position == control.Position) return true;
        return control.ResumePending && failure.EffectiveRecoveryCursor.Position == control.ResumePosition;
    }

    /// <summary>
    /// Checks that a live generation still permits rescheduling.
    /// </summary>
    /// <param name="control">The attempted probe.</param>
    /// <param name="current">The authoritative control.</param>
    /// <returns>Whether a new deadline may be persisted.</returns>
    internal static bool CanReschedule(WebhookProbeState control, WebhookProbeState? current)
    {
        if (current?.Id != control.Id) return false;
        if (control.ResumePending || control.Cancelled) return false;
        return !current.Cancelled;
    }

    static bool HasEligibleDisposition(FailedPartition failure) =>
        failure.WebhookDisposition is WebhookFailureDisposition.Configuration or WebhookFailureDisposition.Retryable;
}
