// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Supervision;

/// <summary>
/// Holds log messages for <see cref="SiloSupervisor"/>.
/// </summary>
internal static partial class SiloSupervisorLogging
{
    [LoggerMessage(LogLevel.Warning, "The supervised check '{Key}' failed; it is run again at its next interval.")]
    internal static partial void CheckingFailed(this ILogger<SiloSupervisor> logger, string key, Exception exception);

    [LoggerMessage(LogLevel.Warning, "The supervised '{Key}' could not run its check after a silo departure; it is asked again on the next pass.")]
    internal static partial void CheckingAfterDepartureFailed(this ILogger<SiloSupervisor> logger, string key, Exception exception);

    [LoggerMessage(LogLevel.Warning, "A pass of checks after a silo departure failed.")]
    internal static partial void DeparturePassFailed(this ILogger<SiloSupervisor> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Looking at the supervisor registry failed; the next tick tries again.")]
    internal static partial void TickFailed(this ILogger<SiloSupervisor> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Opening the window of checks after a silo departure failed.")]
    internal static partial void OpeningDepartureWindowFailed(this ILogger<SiloSupervisor> logger, Exception exception);
}
