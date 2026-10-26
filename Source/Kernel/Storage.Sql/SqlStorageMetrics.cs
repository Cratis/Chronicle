// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Diagnostics;
using Cratis.Diagnostics;
using Cratis.Metrics;

namespace Cratis.Chronicle.Storage.Sql;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class SqlStorageMetrics
{
    [Histogram<double>(WellKnownTelemetryNames.Metrics.SqlSchemaLockWaitDuration, "Time spent waiting for a database schema lock", WellKnownTelemetryNames.Metrics.SqlSchemaLockWaitDurationUnit)]
    internal static partial void SchemaLockWait(this IMeter<SqlStorageTelemetry> meter, double seconds, [Tag(WellKnownTelemetryNames.Attributes.SqlProvider)] string provider, [Tag(WellKnownTelemetryNames.Attributes.SqlSchemaLockOutcome)] string outcome);

    [Counter<int>(WellKnownTelemetryNames.Metrics.SqlLiveQueryPollFailures, "Failed SQL live-query polls; each is retried", WellKnownTelemetryNames.Metrics.SqlLiveQueryPollFailuresUnit)]
    internal static partial void LiveQueryPollFailed(this IMeter<SqlStorageTelemetry> meter, [Tag(WellKnownTelemetryNames.Attributes.SqlLiveQueryName)] string query, [Tag(WellKnownTelemetryNames.Attributes.SqlLiveQueryFailureKind)] string kind);

    [Counter<int>(WellKnownTelemetryNames.Metrics.SqlUpsertRecoveredCollisions, "Concurrent SQL insert collisions recovered by storage", WellKnownTelemetryNames.Metrics.SqlUpsertRecoveredCollisionsUnit)]
    internal static partial void UpsertCollisionRecovered(this IMeter<SqlStorageTelemetry> meter, [Tag(WellKnownTelemetryNames.Attributes.SqlUpsertOperation)] string operation, int count);
}
