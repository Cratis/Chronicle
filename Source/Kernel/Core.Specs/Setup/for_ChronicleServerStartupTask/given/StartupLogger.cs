// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.given;

sealed class StartupLogger(List<string> messages) : ILogger<ChronicleServerStartupTask>
{
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => messages.Add(formatter(state, exception));
}
