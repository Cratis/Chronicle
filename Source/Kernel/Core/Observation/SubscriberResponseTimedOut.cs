// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when Orleans gives up on a call handing events to an observer's subscriber before the
/// configured subscriber timeout elapsed.
/// </summary>
/// <remarks>
/// Kept apart from <see cref="SubscriberCallTimedOut"/> so the log says which timeout fired and how long the call
/// actually ran, rather than naming a subscriber timeout that never took effect. Derives from
/// <see cref="TimeoutException"/> so it classifies as <c language="csharp">FailureKind.Timeout</c>.
/// </remarks>
/// <param name="partition">The <see cref="Key">partition</see> whose events were being handed over.</param>
/// <param name="elapsed">How long the call ran before Orleans gave up on it.</param>
/// <param name="subscriberTimeout">The configured subscriber timeout; zero or less means waiting indefinitely.</param>
/// <param name="innerException">The Orleans <see cref="TimeoutException"/>.</param>
public class SubscriberResponseTimedOut(Key partition, TimeSpan elapsed, TimeSpan subscriberTimeout, Exception innerException)
    : TimeoutException(
        $"The Orleans response timeout fired after {elapsed:g} while handing events for partition '{partition}' to the subscriber, " +
        $"before the subscriber timeout ({Describe(subscriberTimeout)}) elapsed. Subscriber calls are bounded by Orleans at {ObserverSubscriberResponseTimeout.Ceiling:g}",
        innerException)
{
    static string Describe(TimeSpan subscriberTimeout) =>
        subscriberTimeout > TimeSpan.Zero ? subscriberTimeout.ToString("g", System.Globalization.CultureInfo.InvariantCulture) : "wait indefinitely";
}
