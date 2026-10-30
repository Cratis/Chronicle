// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ensuring_indexes;

public sealed class CapturingIndexLogger : ILogger<Sink>
{
    readonly ConcurrentQueue<string> _warnings = new();
    readonly ConcurrentQueue<string> _errors = new();

    public IEnumerable<string> Warnings => _warnings;
    public IEnumerable<string> Errors => _errors;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (logLevel == LogLevel.Warning) _warnings.Enqueue(formatter(state, exception));
        if (logLevel == LogLevel.Error) _errors.Enqueue($"{formatter(state, exception)}: {exception}");
    }
}
