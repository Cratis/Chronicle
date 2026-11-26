// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.given;

/// <summary>
/// Captures the messages the unseen observer removal logs, as an <see cref="ILoggerProvider"/> of the kernel.
/// </summary>
public sealed class CapturedLogs : ILoggerProvider
{
    readonly ConcurrentQueue<(LogLevel Level, string Message)> _entries = new();

    /// <summary>
    /// Gets the messages logged by the removal at the given level so far.
    /// </summary>
    /// <param name="level">The <see cref="LogLevel"/>.</param>
    /// <returns>The messages.</returns>
    public string[] At(LogLevel level) => [.. _entries.Where(entry => entry.Level == level).Select(entry => entry.Message)];

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName) =>
        categoryName.EndsWith("UnseenObserverRemoval", StringComparison.Ordinal) ? new Capture(_entries) : Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    sealed class Capture(ConcurrentQueue<(LogLevel Level, string Message)> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            entries.Enqueue((logLevel, formatter(state, exception)));
    }
}
