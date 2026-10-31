// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

/// <summary>
/// Formats generated log state during the call, before its pooled fields are cleared.
/// </summary>
public sealed class RecordingEventSequenceLogger : ILogger<EventSequence>
{
    readonly ConcurrentQueue<(LogLevel Level, string Message)> _entries = new();

    /// <summary>
    /// Gets the formatted warnings.
    /// </summary>
    public IEnumerable<string> Warnings => _entries.Where(entry => entry.Level == LogLevel.Warning).Select(entry => entry.Message);

    /// <summary>
    /// Gets the formatted errors.
    /// </summary>
    public IEnumerable<string> Errors => _entries.Where(entry => entry.Level == LogLevel.Error).Select(entry => entry.Message);

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        _entries.Enqueue((logLevel, formatter(state, exception)));
}
