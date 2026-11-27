// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <content>
/// Publishes the watchdog's sample to the <see cref="IObserverGauges"/> on the observer's own turn.
/// </content>
/// <remarks>
/// A gauge callback never reads the observer: it reads what was published here, so the published sample has to be
/// replaced at the moment the observer stops being measurable, before anything is awaited. The watchdog sample is the only
/// producer; publishing never takes a reading of its own.
/// </remarks>
public partial class Observer
{
    /// <summary>
    /// Publishes the sample as the observer reads it now, after the watchdog recorded a reading.
    /// </summary>
    void PublishLagGauge()
    {
        if (!EnsureGauge(out var gauge)) return;
        gauge.Publish(gauge.Snapshot with { LagSample = EffectiveLagSample() });
    }

    /// <summary>
    /// Withdraws the published sample, because the observer stopped being measurable or started a catch-up, until the watchdog renews it.
    /// </summary>
    void InvalidateLagGauge()
    {
        if (_gauge is not { } gauge || gauge.Snapshot.LagSample is null) return;
        gauge.Publish(gauge.Snapshot with { LagSample = null });
    }

    /// <summary>
    /// Gets the sample an unrelated snapshot refresh keeps: the published one, while the observer can still be measured.
    /// </summary>
    /// <param name="published">The snapshot that is being replaced.</param>
    /// <returns>The published sample, or null.</returns>
    ObserverLagSample? RetainedLagSample(ObserverGaugeSnapshot published) =>
        FindLagIneligibility() is null ? published.LagSample : null;

    /// <summary>
    /// Gets the sample to publish for what the observer is now. A stalled sample is withheld while a catch-up is under way, not downgraded, so it does not
    /// come back when the catch-up ends but only with a new reading.
    /// </summary>
    /// <returns>The sample, or null when none should be published.</returns>
    ObserverLagSample? EffectiveLagSample() =>
        _lagSample.Status == ObserverLagStatus.Stalled && IsCatchingUpForLag ? null : CurrentLagSample();
}
