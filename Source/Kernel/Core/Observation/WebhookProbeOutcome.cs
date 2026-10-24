// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Classifies delivery outcomes without granting recovery authority.
/// </summary>
internal static class WebhookProbeOutcome
{
    /// <summary>
    /// Requires acknowledgment of exactly the selected event.
    /// </summary>
    /// <param name="result">The subscriber result.</param>
    /// <param name="delivered">The selected event position.</param>
    /// <returns>Whether delivery was acknowledged successfully.</returns>
    internal static bool IsSuccessful(ObserverSubscriberResult result, EventSequenceNumber delivered) =>
        result.State == ObserverSubscriberState.Ok && result.LastSuccessfulObservation == delivered;

    /// <summary>
    /// Describes an unsuccessful probe using the existing metric reasons.
    /// </summary>
    /// <param name="result">The subscriber result.</param>
    /// <returns>The deferral or failure reason.</returns>
    internal static string FailureReason(ObserverSubscriberResult result) => result.State == ObserverSubscriberState.Deferred ? "deferred" : "failed";

    /// <summary>
    /// Distinguishes delivery and local failures from skipped probes.
    /// </summary>
    /// <param name="reason">The reschedule reason.</param>
    /// <returns>Whether the reason counts as a failure.</returns>
    internal static bool IsFailureReason(string reason) => reason == "failed" || reason == "local-error";

    /// <summary>
    /// Maps a receiver recovery policy to its durable failure disposition.
    /// </summary>
    /// <param name="policy">The receiver policy.</param>
    /// <returns>The persisted disposition.</returns>
    internal static WebhookFailureDisposition Disposition(WebhookDeliveryFailurePolicy policy) => policy switch
    {
        WebhookDeliveryFailurePolicy.ParkPartition => WebhookFailureDisposition.Gone,
        WebhookDeliveryFailurePolicy.PauseObserver => WebhookFailureDisposition.Configuration,
        _ => WebhookFailureDisposition.Retryable
    };
}
