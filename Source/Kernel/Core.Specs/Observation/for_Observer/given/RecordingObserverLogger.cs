// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_Observer.given;

/// <summary>
/// Records what an observer logs, and can be told to fail on the messages it is asked to.
/// </summary>
public sealed class RecordingObserverLogger : ILogger<Observer>
{
    readonly ConcurrentQueue<(LogLevel Level, string Message)> _entries = new();

    /// <summary>
    /// Gets or sets a predicate on the formatted message. When it returns true for a message, logging it throws.
    /// </summary>
    public Func<string, bool> ThrowWhen { get; set; } = _ => false;

    /// <summary>
    /// Gets all recorded entries.
    /// </summary>
    public IEnumerable<(LogLevel Level, string Message)> Entries => _entries;

    /// <summary>
    /// Gets the debug messages that were logged.
    /// </summary>
    public IEnumerable<string> Debugs => _entries.Where(_ => _.Level == LogLevel.Debug).Select(_ => _.Message);

    /// <summary>
    /// Gets the warnings that were logged.
    /// </summary>
    public IEnumerable<string> Warnings => _entries.Where(_ => _.Level == LogLevel.Warning).Select(_ => _.Message);

    /// <summary>
    /// Gets the warnings that were logged about suspected skipped events.
    /// </summary>
    public IEnumerable<string> SuspectedSkipWarnings => Warnings.Where(_ => _.Contains("may have skipped", StringComparison.Ordinal));

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

        _entries.Enqueue((logLevel, message));
    }
}
