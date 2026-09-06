// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services;

/// <summary>
/// Log messages the generated service implementations emit.
/// </summary>
/// <remarks>
/// A failing query surfaces on the <see cref="Contracts.Queries.QueryResult{TData}"/> it returns, which tells the
/// caller but leaves no trace on the server. The generated implementations log through here so a failure is still
/// visible in the kernel's own logs, without every service needing its own hand-written log messages.
/// </remarks>
internal static partial class ServiceLogMessages
{
    /// <summary>
    /// Logs a query failing.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to log to.</param>
    /// <param name="exception">The <see cref="Exception"/> that caused the failure.</param>
    /// <param name="service">The service the query belongs to.</param>
    /// <param name="query">The query that failed.</param>
    [LoggerMessage(LogLevel.Error, "The {Query} query on the {Service} service failed")]
    internal static partial void QueryFailed(this ILogger logger, Exception exception, string service, string query);
}
