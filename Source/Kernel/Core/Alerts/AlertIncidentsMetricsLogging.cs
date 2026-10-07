// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Alerts;

internal static partial class AlertIncidentsMetricsLogging
{
    [LoggerMessage(LogLevel.Debug, "Skipping open incident gauge refresh because incident materialization is {State}")]
    internal static partial void RefreshSkipped(this ILogger<AlertIncidentsMetricsOwner> logger, AlertIncidentsReadinessState state);

    [LoggerMessage(LogLevel.Warning, "Failed to refresh the open incident gauge")]
    internal static partial void RefreshFailed(this ILogger<AlertIncidentsMetricsOwner> logger, Exception exception);

    [LoggerMessage(LogLevel.Debug, "Failed to ping the open incident gauge owner")]
    internal static partial void PingFailed(this ILogger<AlertIncidentsMetricsOwnerKeeper> logger, Exception exception);
}
