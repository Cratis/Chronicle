// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Decides how long one reactor or reducer waits before opening its observation stream again after it ended.
/// </summary>
/// <remarks>
/// <para>
/// A fixed delay is right for one observer and wrong for many: seventy reactors whose streams fail together come back
/// together, every two seconds, against a kernel that is still busy - a thundering herd by construction. So each stream
/// that ends without having worked doubles the wait, up to a ceiling, and every wait is jittered so a herd spreads out.
/// </para>
/// <para>
/// The wait starts over once a stream has worked: it delivered something, or it stayed open long enough to have been
/// established. A stream the kernel closes after a long life therefore comes back after the base delay, not a backed-off
/// one. A base delay of zero or less (including <see cref="Timeout.InfiniteTimeSpan"/>) is used as it is.
/// </para>
/// </remarks>
/// <param name="baseDelay">The wait after a stream that had worked.</param>
/// <param name="maxDelay">The longest wait, however often streams end.</param>
/// <param name="healthyStreamDuration">How long a stream must stay open to count as having worked.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/> measuring how long a stream stayed open.</param>
/// <param name="nextJitter">Returns a value in [0, 1) choosing where in the upper half of the wait this one lands.</param>
internal sealed class ObservationReconnectBackoff(
    TimeSpan baseDelay,
    TimeSpan maxDelay,
    TimeSpan healthyStreamDuration,
    TimeProvider timeProvider,
    Func<double> nextJitter)
{
    /// <summary>
    /// The default longest wait between attempts.
    /// </summary>
    internal static readonly TimeSpan DefaultMaxDelay = TimeSpan.FromSeconds(60);

    /// <summary>
    /// The default time a stream must stay open to count as having worked.
    /// </summary>
    /// <remarks>
    /// It must be longer than the kernel takes to give up establishing a stream: the kernel asks a busy observer again on
    /// the same stream, up to four attempts of a thirty-second grain call each, before it fails the stream. A shorter
    /// threshold would count every such stream as having worked and start the wait over at the base delay - exactly for the
    /// herd of slow subscribes the backoff exists for. A stream that has delivered something counts as having worked at once.
    /// </remarks>
    internal static readonly TimeSpan DefaultHealthyStreamDuration = TimeSpan.FromMinutes(3);

    const int MaxExponent = 30;

    int _streamsEndedWithoutWorking;
    long _streamOpenedAt;

    /// <summary>
    /// Records that a new observation stream was opened.
    /// </summary>
    internal void StreamOpened() => Interlocked.Exchange(ref _streamOpenedAt, timeProvider.GetTimestamp());

    /// <summary>
    /// Records that the open stream delivered something, which proves it was established.
    /// </summary>
    internal void StreamDelivered() => Interlocked.Exchange(ref _streamsEndedWithoutWorking, 0);

    /// <summary>
    /// Gets how long to wait before opening the stream again, now that the open one has ended.
    /// </summary>
    /// <returns>The wait.</returns>
    internal TimeSpan NextDelay()
    {
        if (baseDelay <= TimeSpan.Zero)
        {
            return baseDelay;
        }

        if (timeProvider.GetElapsedTime(Interlocked.Read(ref _streamOpenedAt)) >= healthyStreamDuration)
        {
            Interlocked.Exchange(ref _streamsEndedWithoutWorking, 0);
        }

        var exponent = Math.Min(Interlocked.Increment(ref _streamsEndedWithoutWorking) - 1, MaxExponent);
        var ceiling = Math.Min(maxDelay.Ticks, baseDelay.Ticks * Math.Pow(2, exponent));
        var jitter = 0.5 + (0.5 * Math.Clamp(nextJitter(), 0, 1));
        return TimeSpan.FromTicks((long)(ceiling * jitter));
    }
}
