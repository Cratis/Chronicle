// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ProtectedValues;

internal static partial class ProtectedValueReleaseLogging
{
    static readonly ILogger<ProtectedValueRelease> _logger = new TraceWarningLogger();

    /// <summary>Warns that a value was released empty without logging the fence failure's message.</summary>
    /// <param name="exceptionType">The non-personal exception type name.</param>
    internal static void FenceUnavailable(string exceptionType) => FenceUnavailable(_logger, exceptionType);

    [LoggerMessage(Level = LogLevel.Warning, Message = "A protected value was released empty because its erasure fence could not be validated: {ExceptionType}")]
    static partial void FenceUnavailable(ILogger<ProtectedValueRelease> logger, string exceptionType);

    /// <summary>Keeps the existing Trace sink without threading a logger through every release caller.</summary>
    sealed class TraceWarningLogger : ILogger<ProtectedValueRelease>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel)) Trace.TraceWarning(formatter(state, null));
        }
    }
}
