// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.given;

/// <summary>
/// Records what an appended events queue logs, and can be told to fail on the messages it is asked to.
/// </summary>
public sealed class RecordingQueueLogger : ILogger<AppendedEventsQueue>
{
    readonly ConcurrentQueue<(LogLevel Level, string Message, Exception? Exception)> _entries = new();

    /// <summary>
    /// Gets or sets a predicate on the formatted message. When it returns true for a message, logging it throws.
    /// </summary>
    public Func<string, bool> ThrowWhen { get; set; } = _ => false;

    /// <summary>
    /// Gets the warnings about events whose dispatch to an observer failed.
    /// </summary>
    public IEnumerable<string> SuspectedUndeliveredWarnings => _entries
        .Where(_ => _.Level == LogLevel.Warning && _.Message.Contains("may not have been delivered", StringComparison.Ordinal))
        .Select(_ => _.Message);

    /// <summary>
    /// Gets the exceptions that were logged.
    /// </summary>
    public IEnumerable<Exception> Exceptions => _entries.Where(_ => _.Exception is not null).Select(_ => _.Exception!);

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        if (ThrowWhen(message))
        {
            throw new InvalidOperationException("The logger failed");
        }

        _entries.Enqueue((logLevel, message, exception));
    }
}
