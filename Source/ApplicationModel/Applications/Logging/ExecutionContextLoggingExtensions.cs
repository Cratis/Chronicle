// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Serilog;
using Serilog.Configuration;

namespace Aksio.Cratis.Applications.Logging;

/// <summary>
/// Extension methods for <see cref="LoggerEnrichmentConfiguration"/>.
/// </summary>
public static class ExecutionContextLoggingExtensions
{
    /// <summary>
    /// Adds a log enricher that adds values from the execution context to the log event.
    /// </summary>
    /// <param name="enrich"><see cref="LoggerEnrichmentConfiguration"/> to enrich.</param>
    /// <returns><see cref="LoggerConfiguration"/> for builder continuation.</returns>
    public static LoggerConfiguration WithExecutionContext(this LoggerEnrichmentConfiguration enrich)
    {
        return enrich.With<ExecutionContextLogEnricher>();
    }
}
