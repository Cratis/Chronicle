// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents a result object after calling a <see cref="IObserverSubscriber"/>.
/// </summary>
/// <param name="State">The <see cref="ObserverSubscriberState"/>.</param>
/// <param name="ExceptionMessages">Any exception messages associated with the call.</param>
/// <param name="ExceptionStackTrace">Any exception stack trace associated with the call.</param>
public record ObserverSubscriberResult(ObserverSubscriberState State, IEnumerable<string> ExceptionMessages, string ExceptionStackTrace)
{
    /// <summary>
    /// The result that represents a disconnected observer.
    /// </summary>
    public static readonly ObserverSubscriberResult Disconnected = new(ObserverSubscriberState.Disconnected, Enumerable.Empty<string>(), string.Empty);

    /// <summary>
    /// The result that represents a ok observer call.
    /// </summary>
    public static readonly ObserverSubscriberResult Ok = new(ObserverSubscriberState.Ok, Enumerable.Empty<string>(), string.Empty);
}
