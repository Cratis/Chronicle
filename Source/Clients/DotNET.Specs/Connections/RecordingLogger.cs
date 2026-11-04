// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Records rendered log entries and their exceptions for specifications.
/// </summary>
/// <typeparam name="T">The logger category.</typeparam>
public sealed class RecordingLogger<T> : ILogger<T>
{
    readonly List<(LogLevel Level, string Message, Exception? Exception)> _entries = [];

    /// <summary>
    /// Gets the captured entries in logging order.
    /// </summary>
    public IReadOnlyList<(LogLevel Level, string Message, Exception? Exception)> Entries => _entries;

    /// <summary>
    /// Gets entries at the specified level.
    /// </summary>
    /// <param name="level">The level to select.</param>
    /// <returns>The matching entries.</returns>
    public IEnumerable<(LogLevel Level, string Message, Exception? Exception)> AtLevel(LogLevel level) => _entries.Where(_ => _.Level == level);

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        _entries.Add((logLevel, formatter(state, exception), exception));
}
