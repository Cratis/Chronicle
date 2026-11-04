// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Owns the SQL coordination instruments selected once for a database.
/// </summary>
internal sealed class SqlStorageMetrics
{
    static readonly Meter _meter = new(WellKnown.MeterName);
    readonly MetricInstrument<double, HistogramMetricRecorder<double>> _schemaLockWait;
    readonly MetricInstrument<int, CounterMetricRecorder<int>> _liveQueryPollFailed;
    readonly MetricInstrument<int, CounterMetricRecorder<int>> _upsertCollisionRecovered;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlStorageMetrics"/> class.
    /// </summary>
    /// <param name="names">The selected metric naming mode.</param>
    internal SqlStorageMetrics(MetricNames names)
    {
        var instruments = new MetricSet(_meter, names);
        _schemaLockWait = instruments.Histogram<double>(WellKnownMetricNames.SqlSchemaLockWaitDuration, WellKnownMetricNames.SqlSchemaLockWaitDurationUnit, "Time spent waiting for a database schema lock", WellKnownTelemetryAttributes.SqlProvider, WellKnownTelemetryAttributes.SqlSchemaLockOutcome);
        _liveQueryPollFailed = instruments.Counter<int>(WellKnownMetricNames.SqlLiveQueryPollFailures, WellKnownMetricNames.SqlLiveQueryPollFailuresUnit, "Failed SQL live-query polls; each is retried", WellKnownTelemetryAttributes.SqlLiveQueryName, WellKnownTelemetryAttributes.SqlLiveQueryFailureKind);
        _upsertCollisionRecovered = instruments.Counter<int>(WellKnownMetricNames.SqlUpsertRecoveredCollisions, WellKnownMetricNames.SqlUpsertRecoveredCollisionsUnit, "Concurrent SQL insert collisions recovered by storage", WellKnownTelemetryAttributes.SqlUpsertOperation);
    }

    /// <summary>Records a schema lock wait.</summary>
    /// <param name="seconds">The wait duration.</param>
    /// <param name="provider">The provider.</param>
    /// <param name="outcome">The acquisition outcome.</param>
    internal void SchemaLockWait(double seconds, string provider, string outcome) => _schemaLockWait.Record(seconds, provider, outcome);

    /// <summary>Records a failed live-query poll.</summary>
    /// <param name="query">The bounded query name.</param>
    /// <param name="kind">The failure classification.</param>
    internal void LiveQueryPollFailed(string query, string kind) => _liveQueryPollFailed.Record(1, query, kind);

    /// <summary>Records recovered insert collisions.</summary>
    /// <param name="operation">The bounded operation name.</param>
    /// <param name="count">The collision count.</param>
    internal void UpsertCollisionRecovered(string operation, int count) => _upsertCollisionRecovered.Record(count, operation);
}
