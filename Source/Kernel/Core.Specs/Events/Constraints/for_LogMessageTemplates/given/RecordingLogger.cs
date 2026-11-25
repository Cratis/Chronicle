// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Events.Constraints.for_LogMessageTemplates.given;

/// <summary>
/// Captures log entries while their structured state is still available.
/// </summary>
public class RecordingLogger : ILogger<ReindexConstraintsStep>
{
    /// <summary>
    /// Gets the structured values captured during the log call.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, object?>> State { get; private set; } = [];

    /// <summary>
    /// Gets the message rendered by the supplied formatter.
    /// </summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the logged exception.
    /// </summary>
    public Exception? Exception { get; private set; }

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        State = ((IEnumerable<KeyValuePair<string, object?>>)state!).ToArray();
        Message = formatter(state, exception);
        Exception = exception;
    }
}
