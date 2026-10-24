// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Records rendered messages before source-generated logging clears its reusable state.
/// </summary>
/// <typeparam name="T">The logged category.</typeparam>
public sealed class RecordingLogger<T> : ILogger<T>
{
    /// <summary>
    /// Gets the rendered log messages.
    /// </summary>
    public List<string> Messages { get; } = [];

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        Messages.Add(formatter(state, exception));
}
