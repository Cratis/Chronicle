// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.given;

/// <summary>
/// An <see cref="ILogger{T}"/> that keeps the messages it was given, so a spec can assert on what was said.
/// </summary>
/// <typeparam name="T">The type the logger is for.</typeparam>
public class a_logger_that_remembers_messages<T> : ILogger<T>
{
    readonly List<string> _messages = [];
    readonly List<IReadOnlyList<KeyValuePair<string, object?>>> _states = [];
    readonly List<Exception> _exceptions = [];
    readonly List<(LogLevel Level, string Message, Exception? Exception)> _entries = [];

    /// <summary>Gets the messages logged so far.</summary>
    public IReadOnlyList<string> Messages => _messages;

    /// <summary>Gets snapshots of the structured arguments logged so far.</summary>
    public IReadOnlyList<IReadOnlyList<KeyValuePair<string, object?>>> States => _states;

    /// <summary>Gets the actual exception objects logged so far.</summary>
    public IReadOnlyList<Exception> Exceptions => _exceptions;

    /// <summary>Gets the captured entries in logging order.</summary>
    public IReadOnlyList<(LogLevel Level, string Message, Exception? Exception)> Entries => _entries;

    /// <summary>Gets entries at the specified level.</summary>
    /// <param name="level">The level to select.</param>
    /// <returns>The matching entries.</returns>
    public IEnumerable<(LogLevel Level, string Message, Exception? Exception)> AtLevel(LogLevel level) => _entries.Where(_ => _.Level == level);

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        _messages.Add(message);
        _entries.Add((logLevel, message, exception));
        _states.Add(state is IEnumerable<KeyValuePair<string, object?>> arguments
            ? arguments.ToArray()
            : [new("state", state)]);
        if (exception is not null)
        {
            _exceptions.Add(exception);
        }
    }
}
