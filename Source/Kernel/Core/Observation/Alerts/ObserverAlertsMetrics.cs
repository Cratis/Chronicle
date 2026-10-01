// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Cratis.Metrics;

namespace Cratis.Chronicle.Observation.Alerts;

/// <summary>
/// Counts failed attempts to record observer alert transitions.
/// </summary>
internal static class ObserverAlertsMetrics
{
    static readonly Lock _counterLock = new();
    static Counter<int>? _failedTransitions;

    /// <summary>
    /// Records a failed attempt to persist an alert transition.
    /// </summary>
    /// <param name="meter">The observer-scoped meter.</param>
    /// <remarks>
    /// Hand-written until Cratis/Fundamentals#1138 is consumed: the metrics generator passes description as unit.
    /// </remarks>
    internal static void TransitionFailed(this IMeterScope<ObserverAlerts> meter)
    {
        if (meter.Meter is null)
        {
            return;
        }

        Counter<int> counter;
        lock (_counterLock)
        {
            counter = _failedTransitions ??= meter.Meter.CreateCounter<int>(
                "chronicle-alert-transitions-failed",
                unit: null,
                description: "Number of failed attempts to record observer alert transitions");
        }

        var tags = default(TagList);
        foreach (var (key, value) in meter.Tags)
        {
            tags.Add(key, value);
        }

        counter.Add(1, tags);
    }
}
