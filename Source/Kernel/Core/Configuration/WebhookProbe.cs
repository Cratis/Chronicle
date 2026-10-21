// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Controls low-rate real-delivery probes of quarantined webhook observers.
/// </summary>
public class WebhookProbe
{
    /// <summary>Gets whether automatic probing is enabled.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Gets the initial probe delay in seconds.</summary>
    public double InitialDelaySeconds { get; init; } = 60;

    /// <summary>Gets the exponential backoff multiplier.</summary>
    public double BackoffFactor { get; init; } = 2;

    /// <summary>Gets the maximum sampled probe backoff in seconds.</summary>
    public double MaximumDelaySeconds { get; init; } = 3600;

    /// <summary>Gets the symmetric jitter ratio, in the interval [0, 1).</summary>
    public double JitterRatio { get; init; } = 0.2;

    /// <summary>Gets the delivery deadline in seconds.</summary>
    public double TimeoutSeconds { get; init; } = 30;

    /// <summary>Gets whether all probe timing values are usable.</summary>
    internal bool HasValidTiming =>
        double.IsFinite(InitialDelaySeconds) && InitialDelaySeconds > 0 &&
        double.IsFinite(MaximumDelaySeconds) && MaximumDelaySeconds >= InitialDelaySeconds && MaximumDelaySeconds <= TimeSpan.MaxValue.TotalSeconds / 2 &&
        double.IsFinite(BackoffFactor) && BackoffFactor >= 1 &&
        double.IsFinite(JitterRatio) && JitterRatio >= 0 && JitterRatio < 1 &&
        double.IsFinite(TimeoutSeconds) && TimeoutSeconds > 0 && TimeoutSeconds <= int.MaxValue / 1000d;

    /// <summary>
    /// Validates timing settings before they can authorize a delivery.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">A timing setting is invalid.</exception>
    public void Validate()
    {
        if (!HasValidTiming)
        {
            throw new ArgumentOutOfRangeException(nameof(InitialDelaySeconds), "Invalid webhook probe timing settings.");
        }
    }
}
