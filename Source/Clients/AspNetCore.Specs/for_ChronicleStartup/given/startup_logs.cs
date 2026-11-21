// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore.for_ChronicleStartup.given;

public sealed class startup_logs : ILoggerProvider
{
    public ConcurrentQueue<(string Category, string Message, Exception? Error)> Entries { get; } = new();

    public TaskCompletionSource FailureReported { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ILogger CreateLogger(string categoryName) => new startup_logger(this, categoryName);

    public void Dispose()
    {
    }

    sealed class startup_logger(startup_logs owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            owner.Entries.Enqueue((category, formatter(state, exception), exception));
            if (exception is not null)
            {
                owner.FailureReported.TrySetResult();
            }
        }
    }
}
