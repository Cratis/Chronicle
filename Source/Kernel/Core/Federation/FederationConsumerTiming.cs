// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The timing policy of a federation consumer.
/// </summary>
/// <param name="PollInterval">The wait between pulls once the consumer has caught up.</param>
/// <param name="InitialBackoff">The first wait after an error or throttling.</param>
/// <param name="MaxBackoff">The cap of the exponential backoff.</param>
/// <param name="DeniedBackoff">The wait after the producer denied the link.</param>
/// <param name="PullTimeout">The time a single pull may take before the link is considered failed.</param>
public record FederationConsumerTiming(
    TimeSpan PollInterval,
    TimeSpan InitialBackoff,
    TimeSpan MaxBackoff,
    TimeSpan DeniedBackoff,
    TimeSpan PullTimeout)
{
    /// <summary>
    /// Gets the default timing.
    /// </summary>
    public static FederationConsumerTiming Default { get; } = new(
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(60),
        TimeSpan.FromSeconds(90),
        TimeSpan.FromSeconds(30));

    /// <summary>
    /// Gets the next backoff after an error, doubling from <see cref="InitialBackoff"/> up to <see cref="MaxBackoff"/>.
    /// </summary>
    /// <param name="current">The current backoff, zero when the link was healthy.</param>
    /// <returns>The next backoff.</returns>
    public TimeSpan NextBackoff(TimeSpan current) =>
        current < InitialBackoff ? InitialBackoff : TimeSpan.FromTicks(Math.Min(current.Ticks * 2, MaxBackoff.Ticks));
}
