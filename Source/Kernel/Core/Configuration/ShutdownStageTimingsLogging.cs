// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Logs the stages of the silo stop.
/// </summary>
internal static partial class ShutdownStageTimingsLogging
{
    [LoggerMessage(LogLevel.Information, "Silo stop stage {Stage} began at +{Elapsed} after the stop was requested")]
    internal static partial void StageStopBegan(this ILogger<ShutdownStageTimings> logger, string stage, TimeSpan elapsed);

    [LoggerMessage(LogLevel.Information, "Silo stop stage {Stage} stopped in {Duration}")]
    internal static partial void StageStopped(this ILogger<ShutdownStageTimings> logger, string stage, TimeSpan duration);

    [LoggerMessage(LogLevel.Warning, "The shared shutdown drain budget of {Budget} expired while stage {Stage} was still stopping after {Duration}")]
    internal static partial void StageStillStopping(this ILogger<ShutdownStageTimings> logger, string stage, TimeSpan duration, TimeSpan budget);

    [LoggerMessage(LogLevel.Warning, "The shared shutdown drain budget of {Budget} expired before any silo stop stage began stopping")]
    internal static partial void NoStageBeganStopping(this ILogger<ShutdownStageTimings> logger, TimeSpan budget);
}
