// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Extension methods for completing the <see cref="PartitionDeferral"/> of an <see cref="ObserverSubscriberResult"/>.
/// </summary>
public static class ObserverSubscriberResultDeferrals
{
    /// <summary>
    /// Creates a deferred <see cref="ObserverSubscriberResult"/> for a partition an observer asked to have delivered again later.
    /// </summary>
    /// <param name="lastSuccessfulObservation">The last event the observer handled before deferring, or <see cref="EventSequenceNumber.Unavailable"/>.</param>
    /// <param name="retryAfter">How long to wait before delivering the partition again.</param>
    /// <param name="now">The current time.</param>
    /// <returns>The deferred <see cref="ObserverSubscriberResult"/>, resuming from the event after <paramref name="lastSuccessfulObservation"/>.</returns>
    /// <remarks>
    /// When nothing was handled the resume point is not known here; <see cref="ResumingDeferralWithin"/> completes it from the delivered events.
    /// </remarks>
    public static ObserverSubscriberResult Deferred(EventSequenceNumber lastSuccessfulObservation, TimeSpan retryAfter, DateTimeOffset now) =>
        new(ObserverSubscriberState.Deferred, lastSuccessfulObservation, [], string.Empty)
        {
            Deferral = new(
                Guid.NewGuid(),
                PartitionDeferralReason.RequestedByObserver,
                now,
                now + retryAfter,
                lastSuccessfulObservation.IsActualValue ? lastSuccessfulObservation.Next() : EventSequenceNumber.Unavailable)
        };

    /// <summary>
    /// Completes the resume point of a deferred result that does not know it, from the events that were delivered.
    /// </summary>
    /// <param name="result">The <see cref="ObserverSubscriberResult"/> to complete.</param>
    /// <param name="events">The events that were delivered.</param>
    /// <returns>The result, with its deferral resuming from the first delivered event when it had no resume point.</returns>
    public static ObserverSubscriberResult ResumingDeferralWithin(this ObserverSubscriberResult result, IEnumerable<AppendedEvent> events)
    {
        if (result is not { State: ObserverSubscriberState.Deferred, Deferral: { } deferral } || deferral.ResumeFrom.IsActualValue)
        {
            return result;
        }

        var first = events.FirstOrDefault();
        return first is null ? result : result with { Deferral = deferral with { ResumeFrom = first.Context.SequenceNumber } };
    }
}
