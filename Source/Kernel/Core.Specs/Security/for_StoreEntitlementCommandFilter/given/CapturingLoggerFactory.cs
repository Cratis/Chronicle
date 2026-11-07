// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Security.for_StoreEntitlementCommandFilter.given;

public class CapturingLoggerFactory : ILoggerFactory
{
    readonly ConcurrentQueue<string> _messages = new();
    readonly List<(Func<string, bool> Match, TaskCompletionSource Logged)> _waiters = [];

    public IEnumerable<string> Messages => _messages;

    /// <summary>
    /// Completes when a message matching the predicate has been logged, including one logged earlier.
    /// </summary>
    /// <param name="match">Predicate for the message.</param>
    /// <returns>A task that completes when a message matches.</returns>
    public Task WhenLogged(Func<string, bool> match)
    {
        var logged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_waiters)
        {
            if (_messages.Any(match)) logged.TrySetResult();
            else _waiters.Add((match, logged));
        }

        return logged.Task;
    }

    void Capture(string message)
    {
        lock (_waiters)
        {
            _messages.Enqueue(message);
            foreach (var waiter in _waiters.Where(_ => _.Match(message)).ToArray())
            {
                waiter.Logged.TrySetResult();
                _waiters.Remove(waiter);
            }
        }
    }

    public void AddProvider(ILoggerProvider provider)
    {
    }

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(Capture);

    public void Dispose()
    {
    }

    sealed class CapturingLogger(Action<string> capture) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            capture(formatter(state, exception));
    }
}
