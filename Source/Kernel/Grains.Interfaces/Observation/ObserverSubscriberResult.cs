// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents a result object after calling a <see cref="IObserverSubscriber"/>.
/// </summary>
/// <param name="State">The <see cref="ObserverSubscriberState"/>.</param>
/// <param name="LastSuccessfulObservation">The <see cref="EventSequenceNumber"/> of the last successful observation.</param>
/// <param name="ExceptionMessages">Any exception messages associated with the call.</param>
/// <param name="ExceptionStackTrace">Any exception stack trace associated with the call.</param>
public record ObserverSubscriberResult(ObserverSubscriberState State, EventSequenceNumber LastSuccessfulObservation, IEnumerable<string> ExceptionMessages, string ExceptionStackTrace)
{
    /// <summary>
    /// The result that represents a ok observer call.
    /// </summary>
    /// <param name="lastSuccessfulObservation">The <see cref="EventSequenceNumber"/> of the last successful observation.</param>
    /// <returns>The result object to use.</returns>
    public static ObserverSubscriberResult Ok(EventSequenceNumber lastSuccessfulObservation) => new(ObserverSubscriberState.Ok, lastSuccessfulObservation, Enumerable.Empty<string>(), string.Empty);

    /// <summary>
    /// The result that represents a failed observer.
    /// </summary>
    /// <param name="lastSuccessfulObservation">The <see cref="EventSequenceNumber"/> of the last successful observation.</param>
    /// <returns>The result object to use.</returns>
    public static ObserverSubscriberResult Failed(EventSequenceNumber lastSuccessfulObservation) => new(ObserverSubscriberState.Failed, lastSuccessfulObservation, Enumerable.Empty<string>(), string.Empty);

    /// <summary>
    /// The result that represents a disconnected observer.
    /// </summary>
    /// <param name="lastSuccessfulObservation">The <see cref="EventSequenceNumber"/> of the last successful observation.</param>
    /// <returns>The result object to use.</returns>
    public static ObserverSubscriberResult Disconnected(EventSequenceNumber lastSuccessfulObservation) => new(ObserverSubscriberState.Disconnected, lastSuccessfulObservation, Enumerable.Empty<string>(), string.Empty);
}
