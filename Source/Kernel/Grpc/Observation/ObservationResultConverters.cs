// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Converts between the observation results and origins of the contracts and the kernel.
/// </summary>
internal static class ObservationResultConverters
{
    /// <summary>
    /// Converts an observation outcome reported by a client to an <see cref="ObserverSubscriberResult"/>.
    /// </summary>
    /// <param name="state">The <see cref="ObservationState"/> the client reported.</param>
    /// <param name="lastSuccessfulObservation">The last event the client handled successfully.</param>
    /// <param name="exceptionMessages">The exception messages the client reported.</param>
    /// <param name="exceptionStackTrace">The exception stack trace the client reported.</param>
    /// <param name="retryAfterMilliseconds">How long to wait before delivering again, for a deferred outcome.</param>
    /// <param name="clock">The <see cref="TimeProvider"/> to time a deferral from.</param>
    /// <returns>The converted <see cref="ObserverSubscriberResult"/>.</returns>
    public static ObserverSubscriberResult ToObserverSubscriberResult(
        ObservationState state,
        ulong lastSuccessfulObservation,
        IEnumerable<string> exceptionMessages,
        string exceptionStackTrace,
        ulong retryAfterMilliseconds,
        TimeProvider clock) =>
        state == ObservationState.Deferred
            ? ObserverSubscriberResultDeferrals.Deferred(new EventSequenceNumber(lastSuccessfulObservation), TimeSpan.FromMilliseconds(retryAfterMilliseconds), clock.GetUtcNow())
            : new(
                state switch
                {
                    ObservationState.Success => ObserverSubscriberState.Ok,
                    ObservationState.Failed => ObserverSubscriberState.Failed,
                    _ => ObserverSubscriberState.None
                },
                lastSuccessfulObservation,
                exceptionMessages,
                exceptionStackTrace);

    /// <summary>
    /// Converts the outcome of one partition of a page to an <see cref="ObserverSubscriberResult"/>.
    /// </summary>
    /// <param name="result">The <see cref="PartitionResult"/> to convert.</param>
    /// <param name="clock">The <see cref="TimeProvider"/> to time a deferral from.</param>
    /// <returns>The converted <see cref="ObserverSubscriberResult"/>.</returns>
    public static ObserverSubscriberResult ToObserverSubscriberResult(this PartitionResult result, TimeProvider clock) =>
        ToObserverSubscriberResult(
            result.State,
            result.LastSuccessfulObservation,
            result.ExceptionMessages,
            result.ExceptionStackTrace,
            result.RetryAfterMilliseconds,
            clock);

    /// <summary>
    /// Converts a kernel <see cref="Concepts.Observation.ObservationOrigin"/> to its contract.
    /// </summary>
    /// <param name="origin">The <see cref="Concepts.Observation.ObservationOrigin"/> to convert.</param>
    /// <returns>The converted <see cref="ObservationOrigin"/>.</returns>
    public static ObservationOrigin ToContract(this Concepts.Observation.ObservationOrigin origin) =>
        origin switch
        {
            Concepts.Observation.ObservationOrigin.Live => ObservationOrigin.Live,
            Concepts.Observation.ObservationOrigin.CatchUp => ObservationOrigin.CatchingUp,
            Concepts.Observation.ObservationOrigin.Replay => ObservationOrigin.Replaying,
            Concepts.Observation.ObservationOrigin.Retry => ObservationOrigin.Retrying,
            _ => ObservationOrigin.Unknown
        };
}
