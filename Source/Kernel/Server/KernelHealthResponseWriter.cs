// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.SingleWriter;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Writes the readiness status as plain text, naming standby when the kernel is ready only to wait for the lease.
/// </summary>
internal static class KernelHealthResponseWriter
{
    /// <summary>
    /// Writes the health report status.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="report">The health report.</param>
    /// <returns>A task representing the write.</returns>
    internal static Task Write(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "text/plain";
        return context.Response.WriteAsync(StatusOf(report));
    }

    /// <summary>
    /// Gets the status text for a health report.
    /// </summary>
    /// <param name="report">The health report.</param>
    /// <returns>"Standby" in standby, otherwise the aggregated health status.</returns>
    internal static string StatusOf(HealthReport report) =>
        SingleWriterLeaseStandby.IsReportedBy(report) ? SingleWriterLeaseStandby.Status : report.Status.ToString();
}
