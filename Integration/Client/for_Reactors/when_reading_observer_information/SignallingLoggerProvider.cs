// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_Reactors.when_reading_observer_information;

/// <summary>
/// An <see cref="ILoggerProvider"/> that reports when the connection lifecycle logs that it is holding a new
/// generation back until the previous one's disconnect handlers have finished.
/// </summary>
/// <param name="onFenced">Called every time the lifecycle reports it is waiting for a disconnect to complete.</param>
public sealed class SignallingLoggerProvider(Action onFenced) : ILoggerProvider
{
    const string LifecycleCategory = "Cratis.Chronicle.Connections.ConnectionLifecycle";
    const string FenceEvent = "WaitingForDisconnectToComplete";

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName) =>
        categoryName == LifecycleCategory ? new Logger(onFenced) : NullLogger.Instance;

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    sealed class Logger(Action onFenced) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (eventId.Name == FenceEvent)
            {
                onFenced();
            }
        }
    }
}
