// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.SingleWriter;

/// <summary>
/// Describes the standby readiness of a kernel that waits for the single-writer lease before it serves.
/// </summary>
public static class SingleWriterLeaseStandby
{
    /// <summary>
    /// The health check data key that marks a result as standby.
    /// </summary>
    public const string HealthDataKey = "standby";

    /// <summary>
    /// The readiness status written while the kernel is in standby.
    /// </summary>
    public const string Status = "Standby";

    /// <summary>
    /// The message given to callers refused during standby.
    /// </summary>
    public const string RefusalMessage = "Chronicle is in standby waiting for the single-writer lease. Retry shortly.";

    static readonly IReadOnlyDictionary<string, object> _data = new ReadOnlyDictionary<string, object>(new Dictionary<string, object> { [HealthDataKey] = true });

    /// <summary>
    /// Creates the readiness result for standby: degraded, so readiness answers 200 while requests are still refused.
    /// </summary>
    /// <param name="description">What the kernel is waiting for.</param>
    /// <returns>The standby health check result.</returns>
    public static HealthCheckResult Result(string description) => HealthCheckResult.Degraded(description, data: _data);

    /// <summary>
    /// Determines whether a health report describes a kernel in standby.
    /// </summary>
    /// <param name="report">The health report.</param>
    /// <returns>Whether the report is ready only because of standby.</returns>
    public static bool IsReportedBy(HealthReport report) =>
        report.Status != HealthStatus.Unhealthy && report.Entries.Values.Any(_ => _.Data.ContainsKey(HealthDataKey));
}
