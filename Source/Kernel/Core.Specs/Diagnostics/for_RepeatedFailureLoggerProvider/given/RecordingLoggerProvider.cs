// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Diagnostics.for_RepeatedFailureLoggerProvider.given;

public sealed class RecordingLoggerProvider : ILoggerProvider
{
    readonly ConcurrentQueue<(string Category, LogLevel Level, string Message, Exception? Exception)> _entries = new();

    public IReadOnlyList<(string Category, LogLevel Level, string Message, Exception? Exception)> Entries => [.. _entries];

    public ILogger CreateLogger(string categoryName) => new Logger(categoryName, _entries);

    public void Dispose()
    {
    }

    sealed class Logger(string category, ConcurrentQueue<(string, LogLevel, string, Exception?)> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            entries.Enqueue((category, logLevel, formatter(state, exception), exception));
    }
}
