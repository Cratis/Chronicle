// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.given;

/// <summary>
/// A recording <see cref="ILogger{TCategoryName}"/> test double that captures what the sink logs: the level, the
/// rendered message, the structured values and whether an exception was attached.
/// </summary>
/// <remarks>
/// The rendered message and the structured values are what an operator reads and a log aggregator stores, so they
/// are what the specs assert on - both for what a failed write has to name and for what it must never contain.
/// </remarks>
public sealed class RecordingLogger : ILogger<Sink>
{
    readonly List<LogEntry> _entries = [];

    /// <summary>
    /// Gets the entries captured, in the order they were logged.
    /// </summary>
    public IReadOnlyList<LogEntry> Entries => _entries;

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var values = state is IEnumerable<KeyValuePair<string, object?>> pairs
            ? pairs.ToDictionary(_ => _.Key, _ => _.Value)
            : [];
        _entries.Add(new(logLevel, formatter(state, exception), values, exception is not null));
    }

    /// <summary>
    /// Represents one captured log entry.
    /// </summary>
    /// <param name="Level">The <see cref="LogLevel"/> it was logged at.</param>
    /// <param name="Message">The rendered message.</param>
    /// <param name="Values">The structured values, by name.</param>
    /// <param name="HasException">Whether an exception was attached.</param>
    public record LogEntry(LogLevel Level, string Message, IReadOnlyDictionary<string, object?> Values, bool HasException);
}
