// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Maps a measurement to the <see cref="ObserverLag"/> read model.
/// </summary>
internal static class ObserverLagMapping
{
    /// <summary>
    /// Creates the read model of an observer's measurement.
    /// </summary>
    /// <param name="key">The <see cref="ObserverKey"/> of the observer.</param>
    /// <param name="sample">The <see cref="ObserverLagSample"/>.</param>
    /// <returns>The <see cref="ObserverLag"/>.</returns>
    internal static ObserverLag ToLag(this ObserverKey key, ObserverLagSample sample) => new(
        key.EventStore,
        key.Namespace,
        key.EventSequenceId,
        key.ObserverId,
        sample.Status,
        sample.Cause,
        sample.TailEventSequenceNumber,
        sample.NextEventSequenceNumber,
        sample.LastHandledEventSequenceNumber,
        sample.RelevantPendingEventSequenceNumber,
        sample.Distance,
        sample.SampledAt,
        sample.ExpiresAt);
}
