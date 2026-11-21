// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ProtectedValues.given;

/// <summary>
/// Captures rendered messages, structured state and exceptions for protected-value privacy specs.
/// </summary>
/// <typeparam name="T">The logger category.</typeparam>
public sealed class CapturingLogger<T> : ILogger<T>
{
    readonly List<(LogLevel Level, string Message, KeyValuePair<string, object?>[] State, Exception? Exception)> _entries = [];

    /// <summary>
    /// Gets the captured log entries.
    /// </summary>
    public IReadOnlyList<(LogLevel Level, string Message, KeyValuePair<string, object?>[] State, Exception? Exception)> Entries => _entries;

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        _entries.Add((logLevel, formatter(state, exception), state is IEnumerable<KeyValuePair<string, object?>> fields ? fields.ToArray() : [], exception));
}
