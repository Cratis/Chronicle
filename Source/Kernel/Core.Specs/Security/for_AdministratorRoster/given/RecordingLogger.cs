// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Security.for_AdministratorRoster.given;

/// <summary>
/// Logger that formats each entry when it is written, so specs can assert on the operator-facing text.
/// </summary>
public class RecordingLogger : ILogger<AdministratorRoster>
{
    readonly List<(LogLevel Level, string Message)> _entries = [];

    /// <summary>
    /// Gets the formatted messages logged at error level.
    /// </summary>
    public IEnumerable<string> Errors => _entries.Where(entry => entry.Level == LogLevel.Error).Select(entry => entry.Message).ToArray();

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        _entries.Add((logLevel, formatter(state, exception)));
}
