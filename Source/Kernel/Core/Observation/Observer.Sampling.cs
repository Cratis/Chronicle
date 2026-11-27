// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation;

/// <content>
/// Measures the observer's own progress on the watchdog tick, from the reads the watchdog already makes.
/// </content>
/// <remarks>
/// The measurement reads nothing of its own, wakes nothing and writes nothing: a tick that does not reach the watchdog's
/// tail lookup produces no sample, and a sample that is not renewed expires rather than looking current.
/// </remarks>
public partial class Observer
{
    readonly ObserverLagTracker _lagTracker = new();
    ObserverLagSample _lagSample = ObserverLagSample.NotSampled;
    TimeSpan _stallWindow = Observers.DefaultStallWindow;

    /// <summary>
    /// Gets how long a sample describes the observer, which is two watchdog intervals.
    /// </summary>
    TimeSpan LagSampleValidity => TimeSpan.FromSeconds(Math.Max(_watchdogIntervalSeconds, 1) * 2);

    /// <summary>
    /// Gets a value indicating whether tags, event source type or event stream type narrow what is delivered beyond the event types the watchdog probes,
    /// so that an event of an observed type being waiting does not mean one is waiting for this observer.
    /// </summary>
    bool HasAdditionalFilters => _subscription.Filters is { } filters &&
        (filters.Tags.Any() ||
         (filters.EventSourceType is not null && filters.EventSourceType != EventSourceType.Unspecified) ||
         filters.EventStreamType is { IsAll: false });

    void ConfigureLagSampling(Observers config) =>
        _stallWindow = config.StallWindow > TimeSpan.Zero ? config.StallWindow : Observers.DefaultStallWindow;

    /// <summary>
    /// Gets the latest sample of this observer's progress as it reads at the given time.
    /// </summary>
    /// <returns>The <see cref="ObserverLagSample"/>; one that is not measurable when there is none or it has expired.</returns>
    ObserverLagSample CurrentLagSample() => _lagSample.At((timeProvider ?? TimeProvider.System).GetUtcNow());

    /// <summary>
    /// Finds why the observer cannot be measured right now.
    /// </summary>
    /// <returns>The <see cref="ObserverLagCause"/>, or null when the observer can be measured.</returns>
    ObserverLagCause? FindLagIneligibility() => this switch
    {
        _ when _removed || !KeyOwnsDefinition => ObserverLagCause.UnknownState,
        _ when IsQuarantined || State.RunningState != ObserverRunningState.Active => ObserverLagCause.Inactive,
        _ when !_subscription.IsSubscribed => ObserverLagCause.Disconnected,
        _ when State.IsReplaying => ObserverLagCause.ReplayInProgress,
        _ => null
    };

    void RecordLagIneligibility(ObserverLagCause cause)
    {
        var now = (timeProvider ?? TimeProvider.System).GetUtcNow();
        _lagTracker.Reset();
        _lagSample = ObserverLagSample.Unmeasurable(cause, now, now + LagSampleValidity);
    }

    /// <summary>
    /// Records the tail that could not be read.
    /// </summary>
    void RecordLagTailUnavailable() => RecordLagReading(EventSequenceNumber.Unavailable, ObserverLagRelevance.Unknown);

    /// <summary>
    /// Records a reading taken from the facts the watchdog just read, with the cursor as it is after those reads.
    /// </summary>
    /// <param name="tail">The tail the watchdog read.</param>
    /// <param name="relevance">What the watchdog found out about an event the observer observes waiting.</param>
    void RecordLagReading(EventSequenceNumber tail, ObserverLagRelevance relevance)
    {
        // The awaits that produced these facts may have changed what the observer is.
        if (FindLagIneligibility() is { } cause)
        {
            RecordLagIneligibility(cause);
            return;
        }

        var reading = new ObserverLagReading(
            tail,
            State.NextEventSequenceNumber,
            State.LastHandledEventSequenceNumber,
            State.HandledEventCount.Value,
            HasAdditionalFilters ? ObserverLagRelevance.Unknown : relevance,
            State.CatchingUpPartitions.Count > 0 || HasCatchupOwnershipInFlight() || _isPreparingCatchup);

        _lagSample = _lagTracker.Measure(reading, (timeProvider ?? TimeProvider.System).GetUtcNow(), _stallWindow, LagSampleValidity);
    }
}
