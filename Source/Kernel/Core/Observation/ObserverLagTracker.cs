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
        if (GetUnmeasurableCause(reading) is { } cause)
        {
            Reset();
            return ObserverLagSample.Unmeasurable(cause, now, expiresAt);
        }

        var tail = reading.Tail.Value;
        var next = reading.Next.Value;
        ulong? lastHandled = reading.LastHandled.IsActualValue ? reading.LastHandled.Value : null;
        var distance = tail >= next ? tail - next + 1 : 0UL;
        ulong? pending = reading.Relevance.Kind == ObserverLagRelevanceKind.Pending ? reading.Relevance.Position.Value : null;

        ObserverLagSample Sample(ObserverLagStatus status) =>
            new(status, ObserverLagCause.Unknown, tail, next, lastHandled, pending, distance, now, expiresAt);

        RecordReadingTime(now, validity);
        if (distance == 0)
        {
            Reset();
            return Sample(ObserverLagStatus.CaughtUp);
        }

        return Sample(EvaluateLaggingReading(reading, now, stallWindow));
    }

    static ObserverLagCause? GetUnmeasurableCause(ObserverLagReading reading)
    {
        if (!reading.Tail.IsActualValue)
        {
            return ObserverLagCause.TailUnavailable;
        }

        return reading.Next.IsActualValue ? null : ObserverLagCause.UnknownState;
    }

    void RecordReadingTime(DateTimeOffset now, TimeSpan validity)
    {
        var gapTooLong = _lastReadingAt != default && now - _lastReadingAt > validity;
        _lastReadingAt = now;
        if (gapTooLong)
        {
            Reset();
        }
    }

    ObserverLagStatus EvaluateLaggingReading(ObserverLagReading reading, DateTimeOffset now, TimeSpan stallWindow)
    {
        // A catch-up moves the cursor only after a scan, so its progress cannot be judged from the cursor.
        if (reading.Relevance.Kind != ObserverLagRelevanceKind.Pending || reading.IsCatchingUp)
        {
            Reset();
            return ObserverLagStatus.Lagging;
        }

        return IsStalled(reading, now, stallWindow) ? ObserverLagStatus.Stalled : ObserverLagStatus.Lagging;
    }

    bool IsStalled(ObserverLagReading reading, DateTimeOffset now, TimeSpan stallWindow)
    {
        var progress = new Progress(reading.Next, reading.Relevance.Position, reading.LastHandled, reading.HandledEventCount);
        var baseline = _baseline;
        if (baseline?.ShouldRestart(progress, reading.Tail) != false)
        {
            _baseline = new Baseline(now, reading.Tail, progress);
            return false;
        }

        // Compared with the baseline, not the previous reading: a stuck observer must not stop being stalled on a quiet tick.
        return now - baseline.At >= stallWindow && reading.Tail > baseline.Tail;
    }

    readonly record struct Progress(EventSequenceNumber Next, EventSequenceNumber Pending, EventSequenceNumber LastHandled, ulong HandledEventCount);

    sealed record Baseline(DateTimeOffset At, EventSequenceNumber Tail, Progress Progress)
    {
        public bool ShouldRestart(Progress current, EventSequenceNumber tail) => Progress != current || tail < Tail;
    }
}
