// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Orleans.Hosting;

/// <summary>
/// Represents an <see cref="IStartupWorkEscalation"/> that reports through structured logs only.
/// </summary>
/// <param name="logger">The logger.</param>
internal sealed class LoggingStartupWorkEscalation(ILogger<ChronicleServerStartupTask> logger) : IStartupWorkEscalation
{
    /// <inheritdoc/>
    public void Escalate(StartupWorkItem item, int attempts, TimeSpan nextDelay, Exception lastException) =>
        logger.StartupWorkKeepsFailing(lastException, item.Kind, item.Key, attempts, nextDelay);

    /// <inheritdoc/>
    public void Recovered(StartupWorkItem item, int attempts) =>
        logger.StartupWorkRecovered(item.Kind, item.Key, attempts);
}
