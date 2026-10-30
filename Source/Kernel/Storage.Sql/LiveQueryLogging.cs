// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Diagnostics for SQL live-query polling failures and recovery.
/// </summary>
internal static partial class LiveQueryLogging
{
    [LoggerMessage(LogLevel.Warning, "Polling SQL live query '{Query}' failed ({ConsecutiveFailures} in a row); retrying in {RetryDelay}")]
    internal static partial void LiveQueryPollFailed(this ILogger logger, Exception exception, string query, int consecutiveFailures, TimeSpan retryDelay);

    [LoggerMessage(LogLevel.Information, "SQL live query '{Query}' recovered after {FailedPolls} failed polls")]
    internal static partial void LiveQueryRecovered(this ILogger logger, string query, int failedPolls);
}
