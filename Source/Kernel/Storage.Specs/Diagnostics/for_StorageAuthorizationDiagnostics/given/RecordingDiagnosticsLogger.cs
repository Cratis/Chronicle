// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Diagnostics.for_StorageAuthorizationDiagnostics.given;

public sealed class RecordingDiagnosticsLogger : ILogger<StorageAuthorizationDiagnostics>
{
    readonly ConcurrentQueue<(LogLevel Level, string Message, Exception? Exception)> _entries = new();

    public IEnumerable<string> Messages(LogLevel level) => _entries.Where(_ => _.Level == level).Select(_ => _.Message);
    public IEnumerable<Exception> Exceptions => _entries.Where(_ => _.Exception is not null).Select(_ => _.Exception!);

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        _entries.Enqueue((logLevel, formatter(state, exception), exception));
}
