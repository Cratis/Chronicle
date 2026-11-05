// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Webhooks.given;

/// <summary>
/// Records application logs at the complete ILogger boundary.
/// </summary>
/// <typeparam name="T">The logger category.</typeparam>
public sealed class RecordingLogger<T> : ILogger<T>
{
    readonly ConcurrentQueue<RecordedLogEntry> _entries = new();

    /// <summary>
    /// Gets all entries, including disabled-in-production trace messages.
    /// </summary>
    public IEnumerable<RecordedLogEntry> Entries => _entries;

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        _entries.Enqueue(new(logLevel, formatter(state, exception), state is IEnumerable<KeyValuePair<string, object?>> values ? values.ToArray() : [], exception));
}
