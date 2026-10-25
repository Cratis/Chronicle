// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Metrics;

namespace Cratis.Chronicle.Storage.Sql;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class SqlStorageMetrics
{
    [Histogram<double>("chronicle-sql-schema-lock-wait", "Time spent waiting for a database schema lock", "s")]
    internal static partial void SchemaLockWait(this IMeter<SqlStorageTelemetry> meter, double seconds, string provider, string outcome);

    [Counter<int>("chronicle-sql-upsert-collisions-recovered", "Concurrent SQL insert collisions recovered by storage")]
    internal static partial void UpsertCollisionRecovered(this IMeter<SqlStorageTelemetry> meter, string operation, int count);

    [Counter<int>("chronicle-sql-composite-key-writes-refused", "Writes to composite-key read models refused until a replay rebuilds the table with the current key format")]
    internal static partial void CompositeKeyWriteRefused(this IMeter<SqlStorageTelemetry> meter, string reason, int count);

    [Counter<int>("chronicle-sql-composite-key-writes-legacy", "Writes to composite-key read models that were made in the legacy key format")]
    internal static partial void CompositeKeyWriteLegacy(this IMeter<SqlStorageTelemetry> meter, string scope, int count);

    [Counter<int>("chronicle-sql-legacy-composite-key-in-k1-table", "Legacy-format composite key ids found in tables that use the current key format")]
    internal static partial void LegacyCompositeKeyInK1Table(this IMeter<SqlStorageTelemetry> meter, int count);

    [Counter<int>("chronicle-sql-legacy-composite-key-reads", "Reads of composite-key read models that are still in the legacy key format")]
    internal static partial void LegacyCompositeKeyRead(this IMeter<SqlStorageTelemetry> meter, int count);
}
