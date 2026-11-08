// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// A lifecycle logger that completes a signal when the lifecycle reports it is holding a connect back until the
/// previous generation's disconnect handlers have finished.
/// </summary>
public sealed class SignallingLifecycleLogger : ILogger<ConnectionLifecycle>
{
    const string FenceEvent = "WaitingForDisconnectToComplete";

    readonly TaskCompletionSource _fenced = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets a task that completes the first time the lifecycle waits for a disconnect.
    /// </summary>
    public Task Fenced => _fenced.Task;

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (eventId.Name == FenceEvent)
        {
            _fenced.TrySetResult();
        }
    }
}
