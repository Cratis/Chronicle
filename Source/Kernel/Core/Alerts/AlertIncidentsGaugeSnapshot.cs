// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Represents a timestamped, ready-to-publish set of gauge measurements.
/// </summary>
/// <param name="TakenAt">When the snapshot was taken.</param>
/// <param name="Measurements">The measurements, built once at publish time.</param>
public sealed record AlertIncidentsGaugeSnapshot(DateTimeOffset TakenAt, IReadOnlyList<Measurement<long>> Measurements)
{
    /// <summary>
    /// Builds a snapshot from folded values.
    /// </summary>
    /// <param name="takenAt">When the snapshot was taken.</param>
    /// <param name="values">The values to publish.</param>
    /// <returns>The snapshot.</returns>
    internal static AlertIncidentsGaugeSnapshot From(DateTimeOffset takenAt, IEnumerable<(AlertIncidentGaugeBucket Bucket, long Value)> values) =>
        new(takenAt, values.Select(_ => new Measurement<long>(
            _.Value,
            new KeyValuePair<string, object?>("EventStore", _.Bucket.EventStore.Value),
            new KeyValuePair<string, object?>("Namespace", _.Bucket.Namespace.Value),
            new KeyValuePair<string, object?>("ObserverId", _.Bucket.ObserverId.Value),
            new KeyValuePair<string, object?>("EventSequenceId", _.Bucket.EventSequenceId.Value),
            new KeyValuePair<string, object?>("Condition", _.Bucket.Condition.Value),
            new KeyValuePair<string, object?>("Severity", _.Bucket.Severity.ToString()))).ToArray());
}
