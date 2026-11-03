// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_VerifyMongoDBIndexes.given;

/// <summary>
/// A recording <see cref="ILogger{TCategoryName}"/> test double that captures the level and the rendered message of
/// everything it was asked to log.
/// </summary>
/// <typeparam name="T">The category type the logger is for.</typeparam>
/// <remarks>
/// Generated logger messages pass a pooled state that is cleared once logging returns, so the message is rendered
/// while the call is in progress rather than read from the state afterwards.
/// </remarks>
public sealed class RecordingLogger<T> : ILogger<T>
{
    readonly List<(LogLevel Level, string Message)> _entries = [];

    /// <summary>
    /// Gets the entries captured, in the order they were logged.
    /// </summary>
    public IReadOnlyList<(LogLevel Level, string Message)> Entries => _entries;

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        _entries.Add((logLevel, formatter(state, exception)));
}
