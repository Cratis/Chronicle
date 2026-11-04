// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Holds the logger and instruments selected once when a live query is constructed.
/// </summary>
/// <param name="Logger">The logger for poll failures and recovery.</param>
/// <param name="Telemetry">The preselected instruments.</param>
internal sealed record LiveQueryDiagnostics(ILogger Logger, SqlStorageTelemetry Telemetry)
{
    /// <summary>Gets the diagnostics selected for a database.</summary>
    /// <param name="database">The configured database.</param>
    /// <returns>The logger and instruments for its live queries.</returns>
    internal static LiveQueryDiagnostics For(IDatabase database) => new(
        database is Database sql ? sql.LiveQueryLogger : NullLogger.Instance,
        SqlStorageTelemetry.For(database));
}
