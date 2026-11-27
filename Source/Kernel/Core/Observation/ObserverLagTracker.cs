// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Evaluates readings of one observer activation into <see cref="ObserverLagSample"/>s, remembering the baseline a stall is judged against.
/// </summary>
/// <remarks>
/// Not thread safe: it belongs to the grain activation that takes the readings. Nothing here reads storage.
/// </remarks>
public sealed class ObserverLagTracker
{
    Baseline? _baseline;
    DateTimeOffset _lastReadingAt;

    /// <summary>
    /// Forgets the baseline, as when the observer becomes ineligible, so nothing measured before counts toward a stall.
    /// </summary>
    public void Reset() => _baseline = null;

    /// <summary>
    /// Evaluates a reading.
    /// </summary>
    /// <param name="reading">The <see cref="ObserverLagReading"/>.</param>
    /// <param name="now">The time of the reading.</param>
    /// <param name="stallWindow">How long an observer must be seen without progress before it is stalled.</param>
    /// <param name="validity">How long the resulting sample describes the observer.</param>
    /// <returns>The <see cref="ObserverLagSample"/>.</returns>
    public ObserverLagSample Measure(ObserverLagReading reading, DateTimeOffset now, TimeSpan stallWindow, TimeSpan validity)
    {
        var expiresAt = now + validity;
        if (!reading.Tail.IsActualValue)
        {
            Reset();
            return ObserverLagSample.Unmeasurable(ObserverLagCause.TailUnavailable, now, expiresAt);
        }

        if (!reading.Next.IsActualValue)
        {
            Reset();
            return ObserverLagSample.Unmeasurable(ObserverLagCause.UnknownState, now, expiresAt);
        }

        var tail = reading.Tail.Value;
        var next = reading.Next.Value;
        ulong? lastHandled = reading.LastHandled.IsActualValue ? reading.LastHandled.Value : null;
        var distance = tail >= next ? tail - next + 1 : 0UL;
        ulong? pending = reading.Relevance.Kind == ObserverLagRelevanceKind.Pending ? reading.Relevance.Position.Value : null;

        ObserverLagSample Sample(ObserverLagStatus status) =>
            new(status, ObserverLagCause.Unknown, tail, next, lastHandled, pending, distance, now, expiresAt);

        var gapTooLong = _lastReadingAt != default && now - _lastReadingAt > validity;
        _lastReadingAt = now;
        if (gapTooLong) Reset();

        if (distance == 0)
        {
            Reset();
            return Sample(ObserverLagStatus.CaughtUp);
        }

        switch (reading.Relevance.Kind)
        {
            case ObserverLagRelevanceKind.None:
                Reset();
                return Sample(ObserverLagStatus.Lagging);
            case ObserverLagRelevanceKind.Pending:
                break;
            default:
                Reset();
                return Sample(ObserverLagStatus.Lagging);
        }

        // A catch-up moves the cursor only after a scan, so its progress cannot be judged from the cursor.
        if (reading.IsCatchingUp)
        {
            Reset();
            return Sample(ObserverLagStatus.Lagging);
        }

        return IsStalled(reading, now, stallWindow)
            ? Sample(ObserverLagStatus.Stalled)
            : Sample(ObserverLagStatus.Lagging);
    }

    bool IsStalled(ObserverLagReading reading, DateTimeOffset now, TimeSpan stallWindow)
    {
        var pending = reading.Relevance.Position;
        var baseline = _baseline;
        if (baseline is null ||
            baseline.Next != reading.Next ||
            baseline.Pending != pending ||
            baseline.LastHandled != reading.LastHandled ||
            baseline.HandledEventCount != reading.HandledEventCount ||
            reading.Tail < baseline.Tail)
        {
            _baseline = new Baseline(now, reading.Tail, reading.Next, pending, reading.LastHandled, reading.HandledEventCount);
            return false;
        }

        // Compared with the baseline, not the previous reading: a stuck observer must not stop being stalled on a quiet tick.
        return now - baseline.At >= stallWindow && reading.Tail > baseline.Tail;
    }

    record Baseline(DateTimeOffset At, EventSequenceNumber Tail, EventSequenceNumber Next, EventSequenceNumber Pending, EventSequenceNumber LastHandled, ulong HandledEventCount);
}
