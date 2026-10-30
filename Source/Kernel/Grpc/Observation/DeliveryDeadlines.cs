// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Converts a delivery deadline to how it travels to a client.
/// </summary>
internal static class DeliveryDeadlines
{
    /// <summary>
    /// Converts the time the kernel waits for a result to whole milliseconds for the wire.
    /// </summary>
    /// <param name="deliverWithin">The time the kernel waits, or null when it waits without a deadline.</param>
    /// <returns>The milliseconds, at least one when there is a deadline, so zero can keep meaning "no deadline".</returns>
    internal static ulong ToMilliseconds(TimeSpan? deliverWithin) =>
        deliverWithin is { } remaining
            ? (ulong)Math.Max(1, Math.Ceiling(remaining.TotalMilliseconds))
            : 0UL;
}
