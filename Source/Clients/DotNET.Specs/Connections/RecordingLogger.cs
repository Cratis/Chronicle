// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections;

public class RecordingLogger<T>(List<RecordedLog> entries) : ILogger<T>
{
    public RecordingLogger()
        : this([])
    {
    }

    public IReadOnlyList<RecordedLog> Entries => entries;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        entries.Add(new RecordedLog(logLevel, formatter(state, exception), exception));
}
