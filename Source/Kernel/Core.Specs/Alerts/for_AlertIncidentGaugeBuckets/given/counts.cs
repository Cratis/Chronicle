// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Storage.Alerts;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentGaugeBuckets.given;

public class counts : Specification
{
    protected static readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    protected static readonly TimeSpan _retention = TimeSpan.FromMinutes(5);

    protected static AlertIncidentObserverCount Count(AlertSeverity severity, long count, string observer = "observer") =>
        new("store", "Default", observer, "event-log", AlertConditionKind.PartitionFailing, severity, count);

    protected static long ValueOf(AlertIncidentGaugeBuckets buckets, AlertSeverity severity) =>
        buckets.Values.Single(_ => _.Bucket.Severity == severity).Value;
}
