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

    /// <summary>Gets the messages logged so far.</summary>
    public IReadOnlyList<string> Messages => _messages;

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        _messages.Add(formatter(state, exception));
}
